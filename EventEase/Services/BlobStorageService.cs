using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;

namespace EventEase.Services
{
    public interface IBlobStorageService
    {
        /// <summary>
        /// Uploads a file stream to the configured container and returns the public URL.
        /// </summary>
        Task<string> UploadAsync(Stream fileStream, string fileName, string contentType);

        /// <summary>
        /// Deletes a blob by its full URL. Safe to call with null/empty — does nothing.
        /// </summary>
        Task DeleteAsync(string? blobUrl);
    }

    public class BlobStorageService : IBlobStorageService
    {
        private readonly BlobContainerClient _container;

        public BlobStorageService(IConfiguration configuration)
        {
            var connectionString = configuration["AzureStorage:ConnectionString"]
                ?? throw new InvalidOperationException("AzureStorage:ConnectionString is not configured.");

            var containerName = configuration["AzureStorage:ContainerName"] ?? "eventease-images";

            _container = new BlobContainerClient(connectionString, containerName);

            // Ensure the container exists with public blob access so <img src="..."> works
            _container.CreateIfNotExists(PublicAccessType.Blob);
        }

        public async Task<string> UploadAsync(Stream fileStream, string fileName, string contentType)
        {
            // Build a unique blob name to avoid collisions
            var extension = Path.GetExtension(fileName);
            var blobName = $"{Guid.NewGuid()}{extension}";

            var blobClient = _container.GetBlobClient(blobName);

            await blobClient.UploadAsync(fileStream, new BlobHttpHeaders
            {
                ContentType = contentType
            });

            return blobClient.Uri.ToString();
        }

        public async Task DeleteAsync(string? blobUrl)
        {
            if (string.IsNullOrWhiteSpace(blobUrl))
                return;

            // Extract the blob name from the URL — last segment
            if (!Uri.TryCreate(blobUrl, UriKind.Absolute, out var uri))
                return;

            var blobName = Path.GetFileName(uri.LocalPath);
            if (string.IsNullOrWhiteSpace(blobName))
                return;

            var blobClient = _container.GetBlobClient(blobName);
            await blobClient.DeleteIfExistsAsync();
        }
    }
}   