using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.EntityFrameworkCore;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using DocumentManagementApp.Data;
using DocumentManagementApp.Repositories;
using DocumentManagementApp.Services;
using Microsoft.Extensions.Options;
using Microsoft.OpenApi.Models;
using System.Reflection;
using Microsoft.AspNetCore.Builder;
using Microsoft.AspNetCore.Hosting;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;



namespace DocumentManagementApp
{
    /// <summary>
    /// The main entry point for the application.
    /// </summary>
    public class Program
    {
        /// <summary>
        /// The main entry point for the application.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        /// <summary>
        /// Creates a host builder with default configurations.
        /// </summary>
        /// <param name="args">The command-line arguments.</param>
        /// <returns>An IHostBuilder configured with default settings.</returns>
        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("https://localhost:5000");
                });
    }

    /// <summary>
    /// Configures services and the application's request pipeline.
    /// </summary>
    public class Startup
    {
        /// <summary>
        /// Initializes a new instance of the <see cref="Startup"/> class.
        /// </summary>
        /// <param name="configuration">The configuration.</param>
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        /// <summary>
        /// Gets the configuration.
        /// </summary>
        public IConfiguration Configuration { get; }

        /// <summary>
        /// Configures the services for the application.
        /// </summary>
        /// <param name="services">The service collection.</param>
        public void ConfigureServices(IServiceCollection services)
        {
            // Retrieve connection strings and container name from the configuration
            string connectionString = Configuration.GetConnectionString("DefaultConnection") ?? throw new InvalidOperationException("Connection string 'DefaultConnection' not found.");
            string azureBlobStorageConnection = Configuration.GetConnectionString("AzureBlobStorageConnection") ?? throw new InvalidOperationException("Connection string 'AzureBlobStorageConnection' not found.");
            string containerName = Configuration["AzureBlobStorage:ContainerName"] ?? throw new InvalidOperationException("Configuration 'AzureBlobStorage:ContainerName' not found.");

            // Configure and register the DbContext
            services.AddDbContext<DataContext>(options =>
                options.UseNpgsql(connectionString));

            // Register repositories and services
            services.AddScoped<DocumentRepository>();
            services.Configure<AzureBlobStorageOptions>(options =>
            {
                options.ConnectionString = azureBlobStorageConnection;
                options.ContainerName = containerName;
            });
            services.AddScoped<IAzureBlobStorageService, AzureBlobStorageService>();
            services.AddScoped<SASTokenGenerator>(provider =>
            {
                DataContext dbContext = provider.GetRequiredService<DataContext>();
                var azureBlobStorageOptions = provider.GetRequiredService<IOptions<AzureBlobStorageOptions>>().Value;
                if (string.IsNullOrEmpty(azureBlobStorageOptions.ConnectionString))
                {
                    throw new InvalidOperationException("Azure Blob Storage connection string is not configured.");
                }
                if (string.IsNullOrEmpty(azureBlobStorageOptions.ContainerName))
                {
                    throw new InvalidOperationException("Azure Blob Storage container name is not configured.");
                }
                return new SASTokenGenerator(azureBlobStorageOptions.ConnectionString, azureBlobStorageOptions.ContainerName, dbContext);
            });
            services.AddScoped<DocumentService>();

            // Register other services (e.g., controllers, Swagger)
            services.AddControllers();
            
            services.AddSwaggerGen(c =>
            {
                c.SwaggerDoc("v1", new OpenApiInfo
                {
                    Title = "Document Management API",
                    Version = "v1",
                    Description = "API for managing documents",
                    Contact = new OpenApiContact
                    {
                        Name = "Your Name",
                        Email = "your.email@example.com"
                    }
                });

                // Include XML comments if available
                var xmlFile = $"{Assembly.GetExecutingAssembly().GetName().Name}.xml";
                var xmlPath = Path.Combine(AppContext.BaseDirectory, xmlFile);
                if (File.Exists(xmlPath))
                {
                    c.IncludeXmlComments(xmlPath);
                }
            });
        }

        /// <summary>
        /// Configures the HTTP request pipeline.
        /// </summary>
        /// <param name="app">The application builder.</param>
        /// <param name="env">The web host environment.</param>
        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
                // Enable Swagger middleware
                app.UseSwagger();
                app.UseSwaggerUI(c =>
                {
                    c.SwaggerEndpoint("/swagger/v1/swagger.json", "Document Management API v1");
                    c.RoutePrefix = string.Empty; // Set Swagger UI at the app's root
                });
            }

            app.UseRouting();

            app.UseAuthorization();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
