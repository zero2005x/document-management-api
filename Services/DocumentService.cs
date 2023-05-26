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
    public class DocumentService
    {
        private readonly DocumentRepository _documentRepository;
        private readonly IAzureBlobStorageService _blobStorageService;
        private readonly SASTokenGenerator _sasTokenGenerator;

        public DocumentService(DocumentRepository documentRepository, IAzureBlobStorageService blobStorageService, SASTokenGenerator sasTokenGenerator)
        {
            _documentRepository = documentRepository;
            _blobStorageService = blobStorageService;
            _sasTokenGenerator = sasTokenGenerator;
        }

        // Retrieves the list of documents from the repository
        public List<Document> GetDocuments()
        {
            var documents = _documentRepository.GetDocuments();

            // Iterate through the documents and set the FileType property based on the file extension
            foreach (var document in documents)
            {
                document.FileType = GetFileTypeFromExtension(document.FileName);
            }

            return documents.OrderByDescending(d => d.UploadDateTime).ToList(); // Sort the documents by UploadDateTime in descending order
        }

        // Uploads a document to the app
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

        // Saves the document to Azure Blob Storage
        private async Task<string> SaveDocument(IFormFile file, string fileName, string sasToken)
        {
            // Save the document to Azure Blob Storage using the provided file and SAS token
            return await _blobStorageService.SaveDocument(file, fileName, sasToken);
        }

        // Retrieves the file type based on the file extension
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

        // Deletes a document from the app
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

        // Retrieves the document preview data
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

        // Extracts the first page from a PDF document
        private async Task<byte[]> ExtractFirstPageFromPdf(byte[] pdfData)
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
        }

        // Generates a secure download link for a document
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

        // Generates a share link for a document
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

        // Generates a share link for a document with specified validity
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
                return (sasTokenResult.Token, sasTokenResult.ExpirationTime, sasTokenResult.ShareLink);
            }

            return null;
        }

        // Retrieves a document by ID from the repository
        public Document GetDocumentById(int id)
        {
            return _documentRepository.GetDocumentById(id);
        }

        // Removes invalid characters from a file name
        private string RemoveInvalidFileNameCharacters(string fileName)
        {
            var invalidChars = Path.GetInvalidFileNameChars();
            return string.Join("_", fileName.Split(invalidChars, StringSplitOptions.RemoveEmptyEntries));
        }
    }
}
