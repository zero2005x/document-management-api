using System;
using System.Collections.Generic;
using System.IO;
using System.Text.RegularExpressions;
using System.Threading.Tasks;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Sas;
using DocumentManagementApp.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.Extensions.Options;

namespace DocumentManagementApp.Services
{
    public class AzureBlobStorageService : IAzureBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string _containerName;

        public AzureBlobStorageService(IOptions<AzureBlobStorageOptions> options)
        {
            _blobServiceClient = new BlobServiceClient(options.Value.ConnectionString);
            _containerName = options.Value.ContainerName;
        }

        // Retrieves the document bytes from Azure Blob Storage
        public async Task<byte[]> GetDocumentBytes(string filePath)
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            BlobClient blobClient = containerClient.GetBlobClient(filePath);
            BlobDownloadInfo downloadInfo = await blobClient.DownloadAsync().ConfigureAwait(false);

            using (MemoryStream stream = new MemoryStream())
            {
                await downloadInfo.Content.CopyToAsync(stream).ConfigureAwait(false);
                return stream.ToArray();
            }
        }

        // Generates a secure download link for a document in Azure Blob Storage
        public async Task<string> GenerateDownloadLink(string filePath)
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            BlobClient blobClient = containerClient.GetBlobClient(filePath);

            BlobSasBuilder sasBuilder = new BlobSasBuilder()
            {
                BlobContainerName = _containerName,
                BlobName = filePath,
                StartsOn = DateTimeOffset.UtcNow,
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            BlobSasQueryParameters sasQueryParameters = sasBuilder.ToSasQueryParameters(new Azure.Storage.StorageSharedKeyCredential("sealdocuments", "nuCWu7n0HtWUFlFB4nDNAx+PSoDMp44l5iXd7WHZ1dE6lbcsYi/aLQoMje/GVTz1azkJYzgI7y/G+ASt2LlkMg=="));

            UriBuilder blobUriBuilder = new UriBuilder(blobClient.Uri);
            string encodedSasQueryParameters = sasQueryParameters.ToString();

            // Remove existing query parameters from the URL
            if (!string.IsNullOrEmpty(blobUriBuilder.Query))
            {
                string[] queryParameters = blobUriBuilder.Query.TrimStart('?').Split('&');
                List<string> newQueryParameters = new List<string>();
                foreach (string parameter in queryParameters)
                {
                    if (!parameter.StartsWith("sig="))
                    {
                        newQueryParameters.Add(parameter);
                    }
                }
                blobUriBuilder.Query = string.Join("&", newQueryParameters);
            }

            // Append SAS query parameters to the URL
            if (!string.IsNullOrEmpty(encodedSasQueryParameters))
            {
                if (string.IsNullOrEmpty(blobUriBuilder.Query))
                {
                    blobUriBuilder.Query = encodedSasQueryParameters;
                }
                else
                {
                    blobUriBuilder.Query = blobUriBuilder.Query.Substring(1) + "&" + encodedSasQueryParameters;
                }
            }

            return blobUriBuilder.Uri.ToString();
        }

        // Saves a document to Azure Blob Storage
        public async Task<string> SaveDocument(IFormFile file, string fileName, string sasToken)
        {
            if (file == null)
            {
                throw new ArgumentNullException(nameof(file), "File is required.");
            }

            // Remove invalid characters from the file name
            fileName = RemoveInvalidFileNameCharacters(fileName);

            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            BlobClient blobClient = containerClient.GetBlobClient(fileName);

            using (Stream stream = file.OpenReadStream())
            {
                await blobClient.UploadAsync(stream, new BlobUploadOptions { HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType } }).ConfigureAwait(false);
            }

            return fileName;
        }

        // Deletes a document from Azure Blob Storage
        public void DeleteDocument(string filePath)
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            BlobClient blobClient = containerClient.GetBlobClient(filePath);
            blobClient.DeleteIfExists();
        }

        // Retrieves the document preview bytes from Azure Blob Storage
        public async Task<byte[]> GetDocumentPreview(string filePath)
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            BlobClient blobClient = containerClient.GetBlobClient(filePath);
            BlobDownloadInfo downloadInfo = await blobClient.DownloadAsync().ConfigureAwait(false);

            using (MemoryStream stream = new MemoryStream())
            {
                await downloadInfo.Content.CopyToAsync(stream).ConfigureAwait(false);
                return stream.ToArray();
            }
        }

        // Generates a share link for a document in Azure Blob Storage
        public string GenerateShareLink(string filePath)
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            BlobClient blobClient = containerClient.GetBlobClient(filePath);
            return blobClient.Uri.ToString();
        }

        // Removes invalid characters from a file name
        private string RemoveInvalidFileNameCharacters(string fileName)
        {
            // Remove invalid characters from the file name
            string invalidCharsRegex = $"[{Regex.Escape(new string(Path.GetInvalidFileNameChars()))}]";
            return Regex.Replace(fileName, invalidCharsRegex, "");
        }

        public Task<string> SaveDocument(DocumentUploadRequest request, string fileName, string sasToken)
        {
            throw new NotImplementedException();
        }
    }
}
