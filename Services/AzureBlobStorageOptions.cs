namespace DocumentManagementApp.Services
{
    // Class representing the options for Azure Blob Storage
    public class AzureBlobStorageOptions
    {
        // Connection string for Azure Blob Storage
        public string? ConnectionString { get; set; }

        // Container name for Azure Blob Storage
        public string? ContainerName { get; set; }
    }
}
