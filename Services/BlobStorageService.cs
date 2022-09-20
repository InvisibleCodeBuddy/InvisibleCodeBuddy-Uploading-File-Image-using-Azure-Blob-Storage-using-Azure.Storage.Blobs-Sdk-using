using Azure.Storage;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Azure.Storage.Blobs.Specialized;
using Azure.Storage.Sas;
using Microsoft.Extensions.Configuration;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobUploader.Services;

public interface IBlobStorageService
{
    Task<string> UploadFile(string fileName, string containerName, Stream stream, string contentType = null);
    Task<Stream> DownloadFile(string fileName, string containerName);
    string GetSharedAccessSignatureURL(string blobName, string containerName, DateTimeOffset? expirationTime = null);
}

public class BlobStorageService : IBlobStorageService
{ 
    private readonly ILogger<BlobStorageService> _logger;
    private readonly string _storageConnectionString;

    public BlobStorageService(IConfiguration configuration, ILogger<BlobStorageService> logger)
    {
        _logger = logger ?? throw new ArgumentNullException(nameof(logger));
        _storageConnectionString = configuration.GetValue<string>(Constants.StorageAccount) ?? throw new Exception("FilesStorageConnectionString was null");
    }

    public async Task<string> UploadFile(string fileName, string containerName, Stream stream, string contentType = null)
    {
        try
        {
            if (stream != null)
            {
                var container = this.GetContainerReference(containerName);
                // Create the container if it doesn't already exist.
                await container.CreateIfNotExistsAsync();
                //Upload file to cloud
                BlobClient blobClient = container.GetBlobClient(fileName);
                stream.Position = 0;

                if (string.IsNullOrEmpty(contentType))
                    await blobClient.UploadAsync(stream);
                else
                    await blobClient.UploadAsync(stream, new BlobHttpHeaders { ContentType = contentType });


                return blobClient.Name;
            }
        }
        catch (Exception ex)
        {
            _logger.LogError(ex, containerName, Array.Empty<object>());
            Console.WriteLine(ex.StackTrace);
        }
        return string.Empty;
    }

     
    public async Task<Stream> DownloadFile(string fileName, string containerName)
    {

        Stream ms = new MemoryStream();
        BlobContainerClient container = this.GetContainerReference(containerName);
        BlobClient blobClient = container.GetBlobClient(fileName);
        await blobClient.DownloadToAsync(ms);

        return ms;
    }

    public async Task<bool> DeleteFile(string fileName, string containerName)
    {
        BlobContainerClient container = this.GetContainerReference(containerName);
        BlobClient blobClient = container.GetBlobClient(fileName);
        return await blobClient.DeleteIfExistsAsync();
    }
     
    public string GetSharedAccessSignatureURL(string blobName, string containerName, DateTimeOffset? expirationTime = null)
    {  
        BlobSasBuilder sas = new BlobSasBuilder
        {
            // Allow access to blobs
            BlobContainerName = containerName,
            BlobName = blobName, 
            ExpiresOn = expirationTime.HasValue ? expirationTime.Value : DateTimeOffset.UtcNow.AddDays(2)
        };
        sas.SetPermissions(BlobSasPermissions.Read);
        return new BlockBlobClient(_storageConnectionString, containerName, blobName).GenerateSasUri(sas).OriginalString;
         
    }


    private BlobContainerClient GetContainerReference(string containerName)
    {
        BlobContainerClient blobClient = new BlobContainerClient(_storageConnectionString, containerName);
        //Get reference to containers
        return blobClient;
    }


}