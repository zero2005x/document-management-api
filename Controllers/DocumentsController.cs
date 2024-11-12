using System;
using System.IO;
using System.Net;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using DocumentManagementApp.Models;
using DocumentManagementApp.Services;
using DocumentManagementApp.Repositories;

namespace DocumentManagementApp.Controllers
{
    /// <summary>
    /// Controller for managing documents.
    /// </summary>
    [ApiController]
    [Route("api/documents")]
    public class DocumentsController : ControllerBase
    {
        private readonly DocumentService _documentService;
        private readonly IAzureBlobStorageService _blobStorageService;
        private readonly DocumentRepository _documentRepository;
        private readonly SASTokenGenerator _sasTokenGenerator;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentsController"/> class.
        /// </summary>
        /// <param name="documentService">The document service.</param>
        /// <param name="blobStorageService">The blob storage service.</param>
        /// <param name="documentRepository">The document repository.</param>
        /// <param name="sasTokenGenerator">The SAS token generator.</param>
        public DocumentsController(DocumentService documentService, IAzureBlobStorageService blobStorageService, DocumentRepository documentRepository, SASTokenGenerator sasTokenGenerator)
        {
            _documentService = documentService;
            _blobStorageService = blobStorageService;
            _documentRepository = documentRepository;
            _sasTokenGenerator = sasTokenGenerator;
        }

        // GET api/documents
        /// <summary>
        /// Retrieves all documents.
        /// </summary>
        /// <returns>A list of documents.</returns>
        [HttpGet]


        public IActionResult GetDocuments()
        {
            var documents = _documentService.GetDocuments();
            return Ok(documents);
        }

        // POST api/documents/upload
        /// <summary>
        /// Uploads a document.
        /// </summary>
        /// <param name="file">The file to upload.</param>
        /// <param name="name">The name of the document.</param>
        /// <returns>An IActionResult indicating the result of the upload operation.</returns>
        [HttpPost("upload")]

        public async Task<IActionResult> UploadDocument([FromForm] IFormFile file, [FromForm] string name)
        {
            try
            {
                if (file == null || file.Length == 0)
                {
                    return BadRequest("File is required.");
                }

                var documentId = await _documentService.UploadDocument(file, name);
                return Ok(new { DocumentId = documentId });
            }
            catch (Exception ex)
            {
                Console.WriteLine(ex);
                return StatusCode(StatusCodes.Status500InternalServerError, "An error occurred during document upload.");
            }
        }

        // DELETE api/documents/{id}
        /// <summary>
        /// Deletes a document by its ID.
        /// </summary>
        /// <param name="id">The ID of the document to delete.</param>
        /// <returns>An IActionResult indicating the result of the delete operation.</returns>
        [HttpDelete("{id}")]
        public IActionResult DeleteDocument(int id)
        {
            _documentService.DeleteDocument(id);
            return NoContent();
        }

        // GET api/documents/{id}/preview
        /// <summary>
        /// Retrieves a preview of the document by its ID.
        /// </summary>
        /// <param name="id">The ID of the document to preview.</param>
        /// <returns>An IActionResult containing the document preview.</returns>
        [HttpGet("{id}/preview")]
        public async Task<IActionResult> GetDocumentPreview(int id)
        {
            var previewTask = _documentService.GetDocumentPreview(id);
            var preview = await previewTask;

            if (preview == null)
            {
                return NotFound();
            }

            // Determine the MIME type based on the document file type
            string mimeType;
            if (preview.FileType == "application/pdf")
            {
                mimeType = "image/png"; // We've converted the PDF's first page to a PNG image
            }
            else if (preview.FileType == "image/png")
            {
                mimeType = "image/png";
            }
            else if (preview.FileType == "image/jpeg")
            {
                mimeType = "image/jpeg";
            }
            else if (preview.FileType == "application/vnd.openxmlformats-officedocument.wordprocessingml.document")
            {
                mimeType = "application/vnd.openxmlformats-officedocument.wordprocessingml.document";
            }
            else if (preview.FileType == "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet")
            {
                mimeType = "application/vnd.openxmlformats-officedocument.spreadsheetml.sheet";
            }
            else if (preview.FileType == "application/vnd.openxmlformats-officedocument.presentationml.presentation")
            {
                mimeType = "application/vnd.openxmlformats-officedocument.presentationml.presentation";
            }
            else
            {
                // Set a default MIME type if the file type is unknown
                mimeType = "application/octet-stream";
            }

            // Return the preview data with the specified MIME type
            return preview.PreviewData != null
                ? File(preview.PreviewData, mimeType)
                : (IActionResult)NotFound();
        }

        // GET api/documents/{id}/download
        /// <summary>
        /// Downloads a document by its ID.
        /// </summary>
        /// <param name="id">The ID of the document to download.</param>
        /// <returns>An IActionResult containing the document file.</returns>
        [HttpGet("{id}/download")]

        public async Task<IActionResult> DownloadDocument(int id)
        {
            var document = _documentService.GetDocumentById(id);
            if (document == null)
            {
                return NotFound();
            }

            // Increment the download count
            _documentRepository.IncrementDownloadCount(id);

            if (string.IsNullOrEmpty(document.FilePath))
            {
                return NotFound();
            }

            var fileBytes = await _blobStorageService.GetDocumentBytes(document.FilePath);

            if (fileBytes == null)
            {
                return NotFound();
            }

            var mimeType = document.FileName != null ? GetMimeTypeFromFileName(document.FileName) : "application/octet-stream";

            var stream = new MemoryStream(fileBytes);
            return new FileStreamResult(stream, mimeType)
            {
                FileDownloadName = document.FileName
            };
        }
         // GET api/documents/{id}/download-count
        /// <summary>
        /// Retrieves the download count for a document by its ID.
        /// </summary>
        /// <param name="id">The ID of the document.</param>
        /// <returns>An IActionResult containing the download count.</returns>
       

        [HttpGet("{id}/download-count")]
        public IActionResult GetDownloadCount(int id)
        {
            var count = _documentRepository.GetDownloadCount(id);
            return Ok(new { DownloadCount = count });
        }

        // GET api/documents/{id}/share
        /// <summary>
        /// Generates a share link for a document by its ID.
        /// </summary>
        /// <param name="id">The ID of the document.</param>
        /// <param name="validForHours">The number of hours the share link is valid for.</param>
        /// <param name="shareLinkExpiresInHours">The number of hours until the share link expires.</param>
        /// <returns>An IActionResult containing the share link.</returns>
        [HttpGet("{id}/share")]
        public async Task<IActionResult> GetDocumentShareLink(int id, [FromQuery] int validForHours = 1, [FromQuery] int shareLinkExpiresInHours = 1)
        {
            if (validForHours < 1 || validForHours > 24 || shareLinkExpiresInHours < 1 || shareLinkExpiresInHours > 24)
            {
                return BadRequest("ValidForHours and shareLinkExpiresInHours must be between 1 and 24.");
            }

            var shareLinkResult = await _documentService.GenerateDocumentShareLink(id, validForHours, shareLinkExpiresInHours);
            if (shareLinkResult == null)
            {
                return NotFound();
            }

            var shareLink = shareLinkResult.Value.ShareLink;
            return Ok(new { ShareLink = shareLink });
        }

        // Helper function to get the MIME type based on the file name
        private string GetMimeTypeFromFileName(string fileName)
        {
            // Map file extensions to MIME types or use a library like MimeMapping
            // Example: You can add more mappings as needed
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
    }
}
