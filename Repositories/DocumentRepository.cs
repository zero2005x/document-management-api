using System;
using System.Collections.Generic;
using System.Linq;
using DocumentManagementApp.Models;
using Microsoft.EntityFrameworkCore;
using DocumentManagementApp.Data;

namespace DocumentManagementApp.Repositories
{
    public class DocumentRepository
    {
        private readonly DataContext _dbContext;

        public DocumentRepository(DataContext dbContext)
        {
            _dbContext = dbContext ?? throw new ArgumentNullException(nameof(dbContext));
        }

        // Retrieves all documents from the database
        public List<Document> GetDocuments()
        {
            return _dbContext.Documents?.ToList() ?? new List<Document>();
        }

        // Retrieves a document by its blob name
        public Document GetDocumentByBlobName(string blobName)
        {
            return _dbContext.Documents?.FirstOrDefault(d => d.BlobName == blobName);
        }

        // Retrieves a document by its ID
        public Document GetDocumentById(int id)
        {
            return _dbContext.Documents?.FirstOrDefault(d => d.Id == id);
        }

        // Updates the document metadata in the database
        public async Task UpdateDocument(Document document)
        {
            var existingDocument = _dbContext.Documents.Find(document.Id);
            if (existingDocument == null)
            {
                throw new Exception($"Document with id {document.Id} not found.");
            }

            existingDocument.FilePath = document.FilePath;
            existingDocument.SasToken = document.SasToken;
            // Set other properties as needed

            await _dbContext.SaveChangesAsync();
        }

        // Inserts a new document into the database
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

        // Deletes a document from the database
        public void DeleteDocument(int id)
        {
            var document = GetDocumentById(id);
            if (document != null)
            {
                _dbContext.Documents!.Remove(document);
                _dbContext.SaveChanges();
            }
        }

        // Increments the download count of a document by its ID
        public int IncrementDownloadCount(int id)
        {
            var document = _dbContext.Documents.Find(id);
            if (document != null)
            {
                document.DownloadCount++;
                _dbContext.SaveChanges();
                return document.DownloadCount;
            }

            return -1; // Return -1 or throw an exception if the document is not found
        }

        // Retrieves the download count of a document by its ID
        public int GetDownloadCount(int id)
        {
            var document = _dbContext.Documents.Find(id);
            if (document != null)
            {
                return document.DownloadCount;
            }

            return -1; // Return -1 or throw an exception if the document is not found
        }
    }
}
