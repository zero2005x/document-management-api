using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using DocumentManagementApp.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace DocumentManagementApp.Services
{
    public class SASTokenGenerator
    {
        private readonly string _connectionString;
        private readonly string _containerName;
        private readonly DataContext _dbContext;

        public SASTokenGenerator(string connectionString, string containerName, DataContext dbContext)
        {
            _connectionString = connectionString;
            _containerName = containerName;
            _dbContext = dbContext;
        }

        // Generates a SAS token for the specified document ID with the given validity period and share link expiration
        public async Task<SasTokenResult> GenerateSasToken(int documentId, TimeSpan validFor, TimeSpan shareLinkExpiration)
        {
            // Retrieve the document from the database
            var document = await _dbContext.Documents.FirstOrDefaultAsync(d => d.Id == documentId);
            if (document == null)
            {
                return null;
            }

            // Create a BlobServiceClient and BlobContainerClient
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

            // Get the BlobClient for the document's file name
            var blobClient = containerClient.GetBlobClient(document.FileName);

            // Build the SAS token with the specified permissions and expiration
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = document.FileName,
                ExpiresOn = DateTimeOffset.UtcNow.Add(validFor),
                ContentDisposition = $"attachment; filename=\"{document.FileName}\""
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            // Generate the SAS token URI
            var sasToken = blobClient.GenerateSasUri(sasBuilder);

            // Update the document's SasToken and SasExpirationTime properties in the database
            document.SasToken = sasToken.ToString();
            document.SasExpirationTime = DateTimeOffset.UtcNow.Add(validFor);
            await _dbContext.SaveChangesAsync();

            // Return the SAS token result with the token, expiration time, and generated share link
            return new SasTokenResult
            {
                Token = sasToken.ToString(),
                ExpirationTime = DateTimeOffset.UtcNow.Add(validFor),
                ShareLink = GenerateShareLink(document.FileName, shareLinkExpiration)
            };
        }

        // Generates a share link for the specified file name with the given share link expiration
        private string GenerateShareLink(string fileName, TimeSpan shareLinkExpiration)
        {
            // Create a BlobServiceClient and BlobContainerClient
            var blobServiceClient = new BlobServiceClient(_connectionString);
            var containerClient = blobServiceClient.GetBlobContainerClient(_containerName);

            // Get the BlobClient for the file name
            var blobClient = containerClient.GetBlobClient(fileName);

            // Build the SAS token with the specified permissions and expiration
            var sasBuilder = new BlobSasBuilder
            {
                BlobContainerName = _containerName,
                BlobName = fileName,
                ExpiresOn = DateTimeOffset.UtcNow.Add(shareLinkExpiration)
            };

            sasBuilder.SetPermissions(BlobSasPermissions.Read);

            // Generate the SAS token URI
            var sasToken = blobClient.GenerateSasUri(sasBuilder);

            return sasToken.ToString();
        }
    }

    // Represents the result of generating a SAS token
    public class SasTokenResult
    {
        public string Token { get; set; }
        public DateTimeOffset ExpirationTime { get; set; }
        public string ShareLink { get; set; }
    }
}
