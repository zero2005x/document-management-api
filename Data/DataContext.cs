using Microsoft.EntityFrameworkCore;
using DocumentManagementApp.Models;

namespace DocumentManagementApp.Data
{
    /// <summary>
    /// Represents the database context for the Document Management Application.
    /// </summary>
    public class DataContext : DbContext
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="DataContext"/> class using the specified options.
        /// </summary>
        /// <param name="options">The options to be used by a <see cref="DbContext"/>.</param>
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        /// <summary>
        /// Gets or sets the documents in the database.
        /// </summary>
        public DbSet<Document> Documents { get; set; }

        /// <summary>
        /// Configures the schema needed for the document management context.
        /// </summary>
        /// <param name="modelBuilder">The builder being used to construct the model for this context.</param>
        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            modelBuilder.Entity<Document>(entity =>
            {
                entity.ToTable("Documents");

                entity.HasKey(e => e.Id);

                entity.Property(e => e.BlobName)
                      .HasColumnType("text");

                entity.Property(e => e.DownloadCount)
                      .HasColumnType("integer");

                entity.Property(e => e.FileName)
                      .IsRequired()
                      .HasColumnType("text");

                entity.Property(e => e.FilePath)
                      .HasColumnType("text");

                entity.Property(e => e.FileType)
                      .HasColumnType("text");

                entity.Property(e => e.LastModifiedDateTime)
                      .HasColumnType("timestamp with time zone");

                entity.Property(e => e.Name)
                      .HasColumnType("text");

                entity.Property(e => e.SasExpirationTime)
                      .HasColumnType("timestamp with time zone");

                entity.Property(e => e.SasToken)
                      .IsRequired()
                      .HasColumnType("text");

                entity.Property(e => e.ShareLink)
                      .HasColumnType("text");

                entity.Property(e => e.PreviewData)
                      .HasColumnType("bytea");
            });

            base.OnModelCreating(modelBuilder);
        }
    }
}