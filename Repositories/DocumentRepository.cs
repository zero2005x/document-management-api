using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using DocumentManagementApp.Models;
using Microsoft.EntityFrameworkCore;
using DocumentManagementApp.Data;

namespace DocumentManagementApp.Repositories
{
    /// <summary>
    /// Repository class for handling database operations related to <see cref="Document"/> entities.
    /// </summary>
    public class DocumentRepository
    {
        private readonly DataContext _dbContext;

        /// <summary>
        /// Initializes a new instance of the <see cref="DocumentRepository"/> class.
        /// </summary>
        /// <param name="dbContext">The database context to be used for data operations.</param>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="dbContext"/> is null.</exception>
        public DocumentRepository(DataContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        /// <summary>
        /// Retrieves all documents from the database.
        /// </summary>
        /// <returns>A list of all <see cref="Document"/> entities.</returns>
        public List<Document> GetDocuments()
        {
            return _dbContext.Documents?.ToList() ?? new List<Document>();
        }

        /// <summary>
        /// Retrieves a document by its blob name.
        /// </summary>
        /// <param name="blobName">The blob name of the document.</param>
        /// <returns>The <see cref="Document"/> with the specified blob name, or a new <see cref="Document"/> if not found.</returns>
        public Document GetDocumentByBlobName(string blobName)
        {
            return _dbContext.Documents?.FirstOrDefault(d => d.BlobName == blobName) ?? new Document();
        }

        /// <summary>
        /// Retrieves a document by its ID.
        /// </summary>
        /// <param name="id">The ID of the document.</param>
        /// <returns>The <see cref="Document"/> with the specified ID, or a new <see cref="Document"/> if not found.</returns>
        public Document GetDocumentById(int id)
        {
            return _dbContext.Documents?.FirstOrDefault(d => d.Id == id) ?? new Document();
        }

        /// <summary>
        /// Updates the document metadata in the database.
        /// </summary>
        /// <param name="document">The <see cref="Document"/> entity to update.</param>
        /// <exception cref="Exception">Thrown when the document is not found.</exception>
        public async Task UpdateDocument(Document document)
        {
            var existingDocument = _dbContext.Documents!.Find(document.Id);
            if (existingDocument == null)
            {
                throw new Exception($"Document with id {document.Id} not found.");
            }

            existingDocument.FilePath = document.FilePath;
            existingDocument.SasToken = document.SasToken;
            // Set other properties as needed

            await _dbContext.SaveChangesAsync();
        }

        /// <summary>
        /// Inserts a new document into the database.
        /// </summary>
        /// <param name="document">The <see cref="Document"/> entity to insert.</param>
        /// <returns>The ID of the inserted document.</returns>
        /// <exception cref="ArgumentNullException">Thrown when <paramref name="document"/> is null.</exception>
        public async Task<int> InsertDocument(Document document)
        {
            if (document == null)
                throw new ArgumentNullException(nameof(document));

            document.UploadDateTime = DateTime.UtcNow; // Set the UploadDateTime to the current UTC time
            document.LastModifiedDateTime = DateTime.UtcNow; // Set the LastModifiedDateTime to the current UTC time

            _dbContext.Documents!.Add(document);
            await _dbContext.SaveChangesAsync();
            return document.Id;
        }

        /// <summary>
        /// Deletes a document from the database.
        /// </summary>
        /// <param name="id">The ID of the document to delete.</param>
        public void DeleteDocument(int id)
        {
            var document = GetDocumentById(id);
            if (document != null)
            {
                _dbContext.Documents!.Remove(document);
                _dbContext.SaveChanges();
            }
        }

        /// <summary>
        /// Increments the download count of a document by its ID.
        /// </summary>
        /// <param name="id">The ID of the document.</param>
        /// <returns>The updated download count, or -1 if the document is not found.</returns>
        public int IncrementDownloadCount(int id)
        {
            var document = _dbContext.Documents?.Find(id);
            if (document != null)
            {
                document.DownloadCount++;
                _dbContext.SaveChanges();
                return document.DownloadCount;
            }

            return -1; // Return -1 if the document is not found
        }

        /// <summary>
        /// Retrieves the download count of a document by its ID.
        /// </summary>
        /// <param name="id">The ID of the document.</param>
        /// <returns>The download count, or -1 if the document is not found.</returns>
        public int GetDownloadCount(int id)
        {
            var document = _dbContext.Documents?.Find(id);
            if (document != null)
            {
                return document.DownloadCount;
            }

            return -1; // Return -1 if the document is not found
        }
    }
}
