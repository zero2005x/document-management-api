using System;
using System.Threading.Tasks;
using DocumentManagementApp.Models;
using Microsoft.AspNetCore.Http;

namespace DocumentManagementApp.Services
{
    /// <summary>
    /// Defines the contract for interacting with Azure Blob Storage, including operations
    /// for saving, deleting, and retrieving documents and their previews.
    /// </summary>
    public interface IAzureBlobStorageService
    {
        /// <summary>
        /// Saves a document to Azure Blob Storage using the provided upload request, file name, and SAS token.
        /// </summary>
        /// <param name="request">The document upload request containing necessary metadata.</param>
        /// <param name="fileName">The name to assign to the file in storage.</param>
        /// <param name="sasToken">The Shared Access Signature (SAS) token for authentication.</param>
        /// <returns>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the <paramref name="request"/>, <paramref name="fileName"/>, or <paramref name="sasToken"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the document cannot be saved to Azure Blob Storage.
        /// </exception>
        Task<string> SaveDocument(DocumentUploadRequest request, string fileName, string sasToken);

        /// <summary>
        /// Saves a document to Azure Blob Storage using the provided file, file name, and SAS token.
        /// </summary>
        /// <param name="file">The file to be uploaded.</param>
        /// <param name="fileName">The name to assign to the file in storage.</param>
        /// <param name="sasToken">The Shared Access Signature (SAS) token for authentication.</param>
        /// <returns>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the <paramref name="file"/>, <paramref name="fileName"/>, or <paramref name="sasToken"/> is null.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the document cannot be saved to Azure Blob Storage.
        /// </exception>
        Task<string> SaveDocument(IFormFile file, string fileName, string sasToken);

        /// <summary>
        /// Deletes a document from Azure Blob Storage using the specified file path.
        /// </summary>
        /// <param name="filePath">The path of the file to delete in Azure Blob Storage.</param>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the <paramref name="filePath"/> is null or empty.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the document cannot be deleted from Azure Blob Storage.
        /// </exception>
        void DeleteDocument(string filePath);

        /// <summary>
        /// Generates a secure download link for a document stored in Azure Blob Storage.
        /// </summary>
        /// <param name="filePath">The path of the file in Azure Blob Storage.</param>
        /// <returns>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the <paramref name="filePath"/> is null or empty.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the download link cannot be generated.
        /// </exception>
        Task<string> GenerateDownloadLink(string filePath);

        /// <summary>
        /// Generates a shareable link for a document stored in Azure Blob Storage.
        /// </summary>
        /// <param name="filePath">The path of the file in Azure Blob Storage.</param>
        /// <returns>
        /// A shareable link as a string.
        /// Returns <c>null</c> if the share link cannot be generated.
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the <paramref name="filePath"/> is null or empty.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the share link cannot be generated.
        /// </exception>
        string GenerateShareLink(string filePath);

        /// <summary>
        /// Retrieves the byte array of a document stored in Azure Blob Storage.
        /// </summary>
        /// <param name="filePath">The path of the file in Azure Blob Storage.</param>
        /// <returns>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the <paramref name="filePath"/> is null or empty.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the document cannot be retrieved.
        /// </exception>
        Task<byte[]> GetDocumentBytes(string filePath);

        /// <summary>
        /// Retrieves the byte array of a document's preview stored in Azure Blob Storage.
        /// </summary>
        /// <param name="filePath">The path of the preview file in Azure Blob Storage.</param>
        /// <returns>
        /// </returns>
        /// <exception cref="ArgumentNullException">
        /// Thrown if the <paramref name="filePath"/> is null or empty.
        /// </exception>
        /// <exception cref="InvalidOperationException">
        /// Thrown if the preview cannot be retrieved.
        /// </exception>
        Task<byte[]> GetDocumentPreview(string filePath);
    }
}
