using Azure.Storage.Blobs;
using Azure.Storage.Sas;
using DocumentManagementApp.Data;
using Microsoft.EntityFrameworkCore;
using System;
using System.Threading.Tasks;

namespace DocumentManagementApp.Services
{
    /// <summary>
    /// Generates SAS tokens for accessing documents in Azure Blob Storage.
    /// </summary>
    public class SASTokenGenerator
    {
        private readonly string _connectionString;
        private readonly string _containerName = string.Empty;
        private readonly DataContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="SASTokenGenerator"/> class.
        /// </summary>
        /// <param name="connectionString">The connection string to the Azure Blob Storage account.</param>
        /// <param name="containerName">The name of the container in Azure Blob Storage.</param>
        /// <param name="dbContext">The database context for accessing document data.</param>
        public SASTokenGenerator(string connectionString, string containerName, DataContext dbContext)
        {
            _connectionString = connectionString;
            _containerName = containerName;
            _dbContext = dbContext;
        }

        // Generates a SAS token for the specified document ID with the given validity period and share link expiration
        /// <summary>
        /// Generates a SAS token for the specified document ID with the given validity period and share link expiration.
        /// </summary>
        /// <param name="documentId">The ID of the document.</param>
        /// <param name="validFor">The validity period of the SAS token.</param>
        /// <param name="shareLinkExpiration">The expiration time for the share link.</param>
        /// <returns>A task that represents the asynchronous operation. The task result contains the SAS token result.</returns>
        public async Task<SasTokenResult?> GenerateSasToken(int documentId, TimeSpan validFor, TimeSpan shareLinkExpiration)
        {
            // Retrieve the document from the database
            var documents = _dbContext.Documents;
            if (documents == null)
            {
                return null;
            }

            var document = await documents.FirstOrDefaultAsync(d => d.Id == documentId);
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
                ShareLink = document.FileName != null ? GenerateShareLink(document.FileName, shareLinkExpiration) : string.Empty
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
    /// <summary>
    /// Represents the result of generating a SAS token.
    /// </summary>
    public class SasTokenResult
    {
        /// <summary>
        /// Gets or sets the SAS token.
        /// </summary>
        public string Token { get; set; } = string.Empty;
        /// <summary>
        /// Gets or sets the expiration time of the SAS token.
        /// </summary>
        public DateTimeOffset ExpirationTime { get; set; } = DateTimeOffset.MinValue;
        /// <summary>
        /// Gets or sets the share link.
        /// </summary>
        public string ShareLink { get; set; } = string.Empty;
    }
}
