namespace DocumentManagementApp.Models
{
    public class DocumentUploadRequest
    {
        public string? Name { get; set; } // Nullable property to store the document name
        public string FileName { get; set; } // Property to store the document file name

        public string? FileType { get; set; } // Nullable property to store the document file type
        public IFormFile? File { get; set; } // Nullable property to store the document file
    }

    public class DocumentPreview
    {
        public int DocumentId { get; set; } // ID of the document
        public byte[]? PreviewData { get; set; } // Nullable property to store the preview data of the document
        public string? FileType { get; set; } // Nullable property to store the file type of the document

        // Add other properties as needed
    }

    public class Document
    {
        public int Id { get; set; } // ID of the document
        public string? Name { get; set; } // Nullable property to store the document name
        public string? FileType { get; set; } // Nullable property to store the file type of the document
        public string? BlobName { get; set; } // Name of the Blob in Azure Storage

        public string? FilePath { get; set; } // Path of the document file
        public string? FileName { get; set; } // Name of the document file
        public DateTime UploadDateTime { get; set; } // Date and time when the document was uploaded
        public DateTime LastModifiedDateTime { get; set; } // Date and time when the document was last modified
        public int DownloadCount { get; set; } // Number of times the document has been downloaded
        public string? ShareLink { get; set; } // Share link for the document
        public string SasToken { get; set; } // SAS token for the document

        public DateTimeOffset? SasExpirationTime { get; set; } // Expiration time of the SAS token

        // Add other properties as needed
    }
}
