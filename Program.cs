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

namespace DocumentManagementApp
{
    public class Program
    {
        public static void Main(string[] args)
        {
            CreateHostBuilder(args).Build().Run();
        }

        public static IHostBuilder CreateHostBuilder(string[] args) =>
            Host.CreateDefaultBuilder(args)
                .ConfigureWebHostDefaults(webBuilder =>
                {
                    webBuilder.UseStartup<Startup>();
                    webBuilder.UseUrls("https://localhost:5000");
                });
    }

    public class Startup
    {
        public Startup(IConfiguration configuration)
        {
            Configuration = configuration;
        }

        public IConfiguration Configuration { get; }

        public void ConfigureServices(IServiceCollection services)
        {
            // Retrieve connection strings and container name from the configuration
            string connectionString = Configuration.GetConnectionString("DefaultConnection");
            string azureBlobStorageConnection = Configuration.GetConnectionString("AzureBlobStorageConnection");
            string containerName = Configuration["AzureBlobStorage:ContainerName"];

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
                return new SASTokenGenerator(azureBlobStorageOptions.ConnectionString, azureBlobStorageOptions.ContainerName, dbContext);
            });
            services.AddScoped<DocumentService>();

            // Configure CORS
            services.AddCors(options =>
            {
                options.AddPolicy("AllowLocalhost", builder =>
                {
                    builder.WithOrigins("http://localhost:3000")
                        .AllowAnyMethod()
                        .AllowAnyHeader()
                        .WithExposedHeaders("Access-Control-Allow-Origin");
                });
            });

            services.AddControllers();
        }

        public void Configure(IApplicationBuilder app, IWebHostEnvironment env)
        {
            if (env.IsDevelopment())
            {
                app.UseDeveloperExceptionPage();
            }

            app.UseCors("AllowLocalhost"); // Enable CORS with the named policy

            app.UseRouting();

            app.UseEndpoints(endpoints =>
            {
                endpoints.MapControllers();
            });
        }
    }
}
