using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using DocumentManagementApp.Models;
using DocumentManagementApp.Repositories;
using Microsoft.AspNetCore.Http;
using PdfSharp.Pdf.IO;
using ImageMagick;

namespace DocumentManagementApp.Services
{
    /// <summary>
    /// Provides services for managing documents, including retrieval, upload, deletion, preview generation, and link generation.
    /// </summary>
    public class DocumentService
    {
        private readonly DocumentRepository _documentRepository;
        private readonly IAzureBlobStorageService _blobStorageService;
        private readonly SASTokenGenerator _sasTokenGenerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentService"/> class.
        /// </summary>
        /// <param name="documentRepository">The repository for document metadata operations.</param>
        /// <param name="blobStorageService">The service for interacting with Azure Blob Storage.</param>
        /// <param name="sasTokenGenerator">The generator for SAS tokens.</param>
        public DocumentService(DocumentRepository documentRepository, IAzureBlobStorageService blobStorageService, SASTokenGenerator sasTokenGenerator)
        {
            _documentRepository = documentRepository;
            _blobStorageService = blobStorageService;
            _sasTokenGenerator = sasTokenGenerator;
        }

        /// <summary>
        /// Retrieves the list of all documents, setting their file types based on their extensions and ordering them by upload date in descending order.
        /// </summary>
        /// <returns>A list of <see cref="Document"/> objects.</returns>
        public List<Document> GetDocuments()
        {
            var documents = _documentRepository.GetDocuments();

            // Iterate through the documents and set the FileType property based on the file extension
            foreach (var document in documents)
            {
                if (!string.IsNullOrEmpty(document.FileName))
                {
                    document.FileType = GetFileTypeFromExtension(document.FileName);
                }
            }

            return documents.OrderByDescending(d => d.UploadDateTime).ToList(); // Sort the documents by UploadDateTime in descending order
        }

        /// <summary>
        /// Uploads a new document to the application, saving its metadata and the file itself to Azure Blob Storage.
        /// </summary>
        /// <param name="file">The file to be uploaded.</param>
        /// <param name="name">The name of the document.</param>
        /// <returns>The ID of the newly uploaded document.</returns>
        /// <exception cref="ArgumentNullException">Thrown when the provided file is null or empty.</exception>
        /// <exception cref="Exception">Thrown when inserting the document metadata or generating the SAS token fails.</exception>
        public async Task<int> UploadDocument(IFormFile file, string name)
        {
            if (file == null || file.Length == 0)
            {
                throw new ArgumentNullException(nameof(file), "File is required.");
            }

            string fileExtension = Path.GetExtension(file.FileName);
            string fileName = Guid.NewGuid().ToString() + fileExtension;

            // Remove invalid characters from the file name
            fileName = RemoveInvalidFileNameCharacters(fileName);

            var document = new Document
            {
                Name = name,
                FileName = fileName,
                UploadDateTime = DateTime.UtcNow,
                LastModifiedDateTime = DateTime.UtcNow,
                SasToken = "" // Assign an empty string to SasToken initially
                              // Set other document properties as needed
            };

            document.FileType = GetFileTypeFromExtension(document.FileName);

            // Save document metadata to the repository
            int documentId = await _documentRepository.InsertDocument(document);

            if (documentId == 0)
            {
                throw new Exception("Failed to insert document into the database.");
            }

            // Generate SAS token for the document
            var sasTokenResult = await _sasTokenGenerator.GenerateSasToken(documentId, TimeSpan.FromHours(4), TimeSpan.FromDays(7));

            if (sasTokenResult == null)
            {
                throw new Exception($"Failed to generate SAS token for document with ID: {documentId}.");
            }

            var sasToken = sasTokenResult.Token;

            // Save document to Azure Blob Storage using the provided file and SAS token
            string filePath = await SaveDocument(file, fileName, sasToken);

            document.FilePath = filePath;

            // Update the document metadata in the repository with the file path
            await _documentRepository.UpdateDocument(document);

            return documentId;
        }

        /// <summary>
        /// Deletes a document from the application, removing it from both Azure Blob Storage and the repository.
        /// </summary>
        /// <param name="id">The ID of the document to delete.</param>
        public void DeleteDocument(int id)
        {
            var document = _documentRepository.GetDocumentById(id);
            if (document != null)
            {
                if (document.FilePath != null)
                {
                    // Delete document from Azure Blob Storage
                    _blobStorageService.DeleteDocument(document.FilePath);
                }

                // Delete document metadata from the repository
                _documentRepository.DeleteDocument(id);
            }
        }

        /// <summary>
        /// Retrieves the preview data for a specified document, supporting various file types.
        /// </summary>
        /// <param name="id">The ID of the document.</param>
        /// <returns>A <see cref="DocumentPreview"/> object containing preview data, or <c>null</c> if not available.</returns>
        public async Task<DocumentPreview?> GetDocumentPreview(int id)
        {
            var supportedFileTypes = new List<string>
            {
                "application/pdf",
                "image/png",
                "image/jpeg",
                "application/vnd.openxmlformats-officedocument.wordprocessingml.document",
                "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet",
                "application/vnd.openxmlformats-officedocument.presentationml.presentation"
            };

            var document = _documentRepository.GetDocumentById(id);
            if (document != null && document.FileType != null && supportedFileTypes.Contains(document.FileType))
            {
                if (document.FilePath != null)
                {
                    // Fetch document preview data from Azure Blob Storage or generate thumbnail
                    var previewData = await _blobStorageService.GetDocumentPreview(document.FilePath);

                    if (document.FileType == "application/pdf")
                    {
                        // Process PDF document to extract the first page
                        byte[] firstPageData = await ExtractFirstPageFromPdf(previewData);
                        previewData = firstPageData;
                    }

                    return new DocumentPreview
                    {
                        DocumentId = document.Id,
                        PreviewData = previewData,
                        FileType = document.FileType // Added FileType to DocumentPreview
                    };
                }
            }

            return null;
        }

        /// <summary>
        /// Generates a secure download link for a specified document.
        /// </summary>
        /// <param name="id">The ID of the document.</param>
        /// <returns>A secure download link as a string, or <c>null</c> if the document does not exist.</returns>
        public async Task<string?> GetDocumentDownloadLink(int id)
        {
            var document = _documentRepository.GetDocumentById(id);
            if (document != null && document.FilePath != null)
            {
                // Generate secure download link for the document
                return await _blobStorageService.GenerateDownloadLink(document.FilePath);
            }

            return null;
        }

        /// <summary>
        /// Generates a shareable link for a specified document that is valid for a given duration.
        /// </summary>
        /// <param name="id">The ID of the document.</param>
        /// <param name="validFor">The time span for which the share link is valid.</param>
        /// <returns>A shareable link as a string, or <c>null</c> if the document does not exist.</returns>
        public async Task<string?> GetDocumentShareLink(int id, TimeSpan validFor)
        {
            var document = _documentRepository.GetDocumentById(id);
            if (document != null && document.FilePath != null)
            {
                // Generate secure download link for the document
                string shareLink = await _blobStorageService.GenerateDownloadLink(document.FilePath);
                return shareLink;
            }

            return null;
        }

        /// <summary>
        /// Generates a shareable link along with its token and expiration time for a specified document.
        /// </summary>
        /// <param name="id">The ID of the document.</param>
        /// <param name="validForHours">The number of hours the SAS token is valid for.</param>
        /// <param name="shareLinkExpiresInHours">The number of hours the share link remains valid.</param>
        /// <returns>
        /// A tuple containing the SAS token, its expiration time, and the share link, or <c>null</c> if the document does not exist.
        /// </returns>
        /// <exception cref="ArgumentException">Thrown when the validity durations are out of the allowed range.</exception>
        public async Task<(string Token, DateTimeOffset ExpirationTime, string ShareLink)?> GenerateDocumentShareLink(int id, int validForHours, int shareLinkExpiresInHours)
        {
            if (validForHours < 1 || validForHours > 24 || shareLinkExpiresInHours < 1 || shareLinkExpiresInHours > 24)
            {
                throw new ArgumentException("Invalid validForHours or shareLinkExpiresInHours value.");
            }

            var validFor = TimeSpan.FromHours(validForHours);
            var shareLinkExpiration = TimeSpan.FromHours(shareLinkExpiresInHours);

            var document = _documentRepository.GetDocumentById(id);
            if (document != null && document.FilePath != null)
            {
                // Generate the share link using the SASTokenGenerator
                var sasTokenResult = await _sasTokenGenerator.GenerateSasToken(document.Id, validFor, shareLinkExpiration);
                if (sasTokenResult == null)
                {
                    throw new Exception($"Failed to generate SAS token for document with ID: {id}.");
                }
                return (sasTokenResult.Token, sasTokenResult.ExpirationTime, sasTokenResult.ShareLink);
            }

            return null;
        }

        /// <summary>
        /// Retrieves a document by its ID from the repository.
        /// </summary>
        /// <param name="id">The ID of the document.</param>
        /// <returns>The <see cref="Document"/> object if found; otherwise, <c>null</c>.</returns>
        public Document GetDocumentById(int id)
        {
            return _documentRepository.GetDocumentById(id);
        }

        /// <summary>
        /// Saves a document to Azure Blob Storage.
        /// </summary>
        /// <param name="file">The file to be saved.</param>
        /// <param name="fileName">The name to assign to the file in storage.</param>
        /// <param name="sasToken">The SAS token for authentication.</param>
        /// <returns>The file path in Azure Blob Storage.</returns>
        private async Task<string> SaveDocument(IFormFile file, string fileName, string sasToken)
        {
            // Save the document to Azure Blob Storage using the provided file and SAS token
            return await _blobStorageService.SaveDocument(file, fileName, sasToken);
        }

        /// <summary>
        /// Determines the MIME type of a file based on its extension.
        /// </summary>
        /// <param name="fileName">The name of the file.</param>
        /// <returns>The MIME type as a string.</returns>
        private string GetFileTypeFromExtension(string fileName)
        {
            var extension = Path.GetExtension(fileName)?.ToLowerInvariant();
            switch (extension)
            {
                case ".pdf":
                    return "application/pdf";
                case ".png":
                    return "image/png";
                case ".jpg":
                case ".jpeg":
                    return "image/jpeg";
                case ".docx":
                    return "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
                case ".xlsx":
                    return "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
                case ".pptx":
                    return "application/vnd.openxmlformats-officedocument.presentationml.presentation";
                default:
                    return "application/octet-stream";
            }
        }

        /// <summary>
        /// Extracts the first page from a PDF document and converts it to a PNG image.
        /// </summary>
        /// <param name="pdfData">The byte array representing the PDF document.</param>
        /// <returns>A byte array of the first page as a PNG image.</returns>
        private async Task<byte[]> ExtractFirstPageFromPdf(byte[] pdfData)
        {
            return await Task.Run(() =>
            {
                using (var inputStream = new MemoryStream(pdfData))
                {
                    using (var outputStream = new MemoryStream())
                    {
                        using (var image = new MagickImage())
                        {
                            // Read the PDF data into the MagickImage object
                            image.Read(inputStream);

                            // Set the output format to PNG
                            image.Format = MagickFormat.Png;

                            // Extract the first page and write it to the output stream
                            image.Write(outputStream);
                        }

                        // Return the resulting image data
                        return outputStream.ToArray();
                    }
                }
            });
        }

        /// <summary>
        /// Removes invalid characters from a file name, replacing them with underscores.
        /// </summary>
        /// <param name="fileName">The original file name.</param>
        /// <returns>A sanitized file name with invalid characters removed.</returns>
        private string RemoveInvalidFileNameCharacters(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
