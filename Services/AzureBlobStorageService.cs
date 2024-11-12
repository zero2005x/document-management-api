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
    /// <summary>
    /// Service for managing Azure Blob Storage operations.
    /// </summary>
    public class AzureBlobStorageService : IAzureBlobStorageService
    {
        private readonly BlobServiceClient _blobServiceClient;
        private readonly string? _containerName;

        /// <summary>
        /// Initializes a new instance of the <see cref="AzureBlobStorageService"/> class.
         /// <param name="options">The Azure Blob Storage options.</param>
        /// </summary>
        public AzureBlobStorageService(IOptions<AzureBlobStorageOptions> options)
        {
            var azureBlobStorageOptions = options.Value;
            _blobServiceClient = new BlobServiceClient(azureBlobStorageOptions.ConnectionString);
            _containerName = azureBlobStorageOptions.ContainerName;
        }

        /// <summary>
        /// Retrieves the document bytes from Azure Blob Storage.
        /// </summary>
        /// <param name="filePath">The path of the file in the blob storage.</param>
        /// <returns>A byte array containing the document bytes.</returns>
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

        /// <summary>
        /// Saves a document to Azure Blob Storage.
        /// </summary>
        /// <param name="file">The file to be uploaded.</param>
        /// <param name="fileName">The name of the file in the blob storage.</param>
        /// <param name="sasToken">The SAS token for authentication.</param>
        /// <returns>The name of the saved file.</returns>
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
                await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = file.ContentType }).ConfigureAwait(false);
            }

            return blobClient.Uri.ToString();
        }

        /// <summary>
        /// Generates a secure download link for a document in Azure Blob Storage.
        /// </summary>
        /// <param name="filePath">The path of the file in the blob storage.</param>
        /// <returns>The share link for the document.</returns>
        public Task<string> GenerateDownloadLink(string filePath)
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            BlobClient blobClient = containerClient.GetBlobClient(filePath);
        
            BlobSasBuilder sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = filePath,
                Resource = "b",
                ExpiresOn = DateTimeOffset.UtcNow.AddHours(1)
            };
        
            sasBuilder.SetPermissions(BlobSasPermissions.Read);
        
            Uri sasUri = blobClient.GenerateSasUri(sasBuilder);
            return Task.FromResult(sasUri.ToString());
        }

        /// <summary>
        /// Deletes a document from Azure Blob Storage.
        /// </summary>
        /// <param name="filePath">The path of the file in the blob storage.</param>
        public void DeleteDocument(string filePath)
        {
            BlobContainerClient containerClient = _blobServiceClient.GetBlobContainerClient(_containerName);
            BlobClient blobClient = containerClient.GetBlobClient(filePath);
            blobClient.DeleteIfExists();
        }

        /// <summary>
        /// Retrieves the document preview bytes from Azure Blob Storage.
        /// </summary>
        /// <param name="filePath">The path of the file in the blob storage.</param>
        /// <returns>A byte array containing the document preview bytes.</returns>
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

        /// <summary>
        /// Generates a share link for a document in Azure Blob Storage.
        /// </summary>
        /// <param name="filePath">The path of the file in the blob storage.</param>
        /// <returns>The share link for the document.</returns>
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

        /// <summary>
        /// Saves a document to Azure Blob Storage.
        /// </summary>
        /// <param name="request">The document upload request.</param>
        /// <param name="fileName">The name of the file in the blob storage.</param>
        /// <param name="sasToken">The SAS token for authentication.</param>
        /// <returns>The name of the saved file.</returns>
        public Task<string> SaveDocument(DocumentUploadRequest request, string fileName, string sasToken)
        {
            throw new NotImplementedException();
        }
    }
}
