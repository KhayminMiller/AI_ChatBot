using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.Extensions.Configuration;
using System.IO;
using System.Threading.Tasks;
using System.Collections.Generic;

public class BlobStorage
{
    private readonly BlobContainerClient _containerClient;

    public BlobStorage(IConfiguration config)
    {
        var connectionString = config.GetConnectionString("AzureBlobStorage");
        var containerName = config["AzureStorage:ContainerName"] ?? "faces";

        if (string.IsNullOrWhiteSpace(connectionString))
            throw new Exception("❌ BlobStorage connection string is null or empty.");

        if (string.IsNullOrWhiteSpace(containerName))
            throw new Exception("❌ BlobStorage container name is null or empty.");

        var blobServiceClient = new BlobServiceClient(connectionString);
        _containerClient = blobServiceClient.GetBlobContainerClient(containerName);

        // Auto-create container with public access off
        _containerClient.CreateIfNotExists(PublicAccessType.None);

        Console.WriteLine($"✅ Blob container '{containerName}' created or already exists.");
    }

    public async Task UploadFileAsync(Stream fileStream, string fileName)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);
        await blobClient.UploadAsync(fileStream, overwrite: true);
    }

    public async Task UploadFileAsync(byte[] fileBytes, string fileName)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);
        using var ms = new MemoryStream(fileBytes);
        await blobClient.UploadAsync(ms, overwrite: true);
    }

    public async Task<Stream> DownloadFileAsync(string fileName)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);
        var response = await blobClient.DownloadAsync();
        return response.Value.Content;
    }

    public async Task DeleteFileAsync(string fileName)
    {
        var blobClient = _containerClient.GetBlobClient(fileName);
        await blobClient.DeleteIfExistsAsync();
    }

    public async Task<List<string>> ListFilesAsync()
    {
        var results = new List<string>();
        await foreach (BlobItem blobItem in _containerClient.GetBlobsAsync())
        {
            results.Add(blobItem.Name);
        }
        return results;
    }
}
