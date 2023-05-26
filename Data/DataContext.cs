using Microsoft.EntityFrameworkCore;
using DocumentManagementApp.Models;

namespace DocumentManagementApp.Data
{
    public class DataContext : DbContext
    {
        public DataContext(DbContextOptions<DataContext> options) : base(options)
        {
        }

        public DbSet<Document>? Documents { get; set; } // Add ? to make the property nullable

        protected override void OnModelCreating(ModelBuilder modelBuilder)
        {
            // Configure the document entity and its properties
            modelBuilder.Entity<Document>(entity =>
            {
                entity.HasKey(d => d.Id);
                // Set other property configurations
            });
        }
    }
}
