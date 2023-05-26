using System.Threading.Tasks;
using DocumentManagementApp.Models;

namespace DocumentManagementApp.Services
{
    // Interface defining the contract for Azure Blob Storage service
    public interface IAzureBlobStorageService
    {
        // Saves a document using the provided document upload request, file name, and SAS token
        Task<string> SaveDocument(DocumentUploadRequest request, string fileName, string sasToken);

        // Deletes a document with the specified file path
        void DeleteDocument(string filePath);

        // Generates a secure download link for a document with the specified file path
        Task<string> GenerateDownloadLink(string filePath);

        // Generates a shareable link for a document with the specified file path
        string GenerateShareLink(string filePath);

        // Retrieves the byte array of a document with the specified file path
        Task<byte[]> GetDocumentBytes(string filePath);

        // Retrieves the byte array of a document preview with the specified file path
        Task<byte[]> GetDocumentPreview(string filePath);

        // Saves a document using the provided file, file name, and SAS token
        Task<string> SaveDocument(IFormFile file, string fileName, string sasToken);
    }
}
