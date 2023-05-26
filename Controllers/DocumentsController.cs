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
    [ApiController]
    [Route("api/documents")]
    public class DocumentsController : ControllerBase
    {
        private readonly DocumentService _documentService;
        private readonly IAzureBlobStorageService _blobStorageService;
        private readonly DocumentRepository _documentRepository;
        private readonly SASTokenGenerator _sasTokenGenerator;

        public DocumentsController(DocumentService documentService, IAzureBlobStorageService blobStorageService, DocumentRepository documentRepository, SASTokenGenerator sasTokenGenerator)
        {
            _documentService = documentService;
            _blobStorageService = blobStorageService;
            _documentRepository = documentRepository;
            _sasTokenGenerator = sasTokenGenerator;
        }

        // GET api/documents
        [HttpGet]
        public IActionResult GetDocuments()
        {
            var documents = _documentService.GetDocuments();
            return Ok(documents);
        }

        // POST api/documents/upload
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
        [HttpDelete("{id}")]
        public IActionResult DeleteDocument(int id)
        {
            _documentService.DeleteDocument(id);
            return NoContent();
        }

        // GET api/documents/{id}/preview
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

            var fileBytes = await _blobStorageService.GetDocumentBytes(document.FilePath);

            if (fileBytes == null)
            {
                return NotFound();
            }

            var mimeType = GetMimeTypeFromFileName(document.FileName);

            var stream = new MemoryStream(fileBytes);
            return new FileStreamResult(stream, mimeType)
            {
                FileDownloadName = document.FileName
            };
        }

        // GET api/documents/{id}/download-count
        [HttpGet("{id}/download-count")]
        public IActionResult GetDownloadCount(int id)
        {
            var count = _documentRepository.GetDownloadCount(id);
            return Ok(new { DownloadCount = count });
        }

        // GET api/documents/{id}/share
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
