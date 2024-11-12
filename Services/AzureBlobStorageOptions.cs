namespace DocumentManagementApp.Services
{
    /// <summary>
    /// Represents the configuration options for Azure Blob Storage.
    /// </summary>
    public class AzureBlobStorageOptions
    {
        /// <summary>
        /// Gets or sets the connection string for Azure Blob Storage.
        /// </summary>
        public string? ConnectionString { get; set; }

        /// <summary>
        /// Gets or sets the container name for Azure Blob Storage.
        /// </summary>
        public string? ContainerName { get; set; }
    }
}