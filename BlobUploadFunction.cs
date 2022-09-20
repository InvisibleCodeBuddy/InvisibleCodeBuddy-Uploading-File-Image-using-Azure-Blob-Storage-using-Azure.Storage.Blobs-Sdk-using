using BlobUploader.Services;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.WebJobs;
using Microsoft.Azure.WebJobs.Extensions.Http;
using Microsoft.Extensions.Logging;
using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;

namespace BlobUploader;

public class BlobUploadFunction
{
    private readonly ILogger<BlobUploadFunction> _logger;
    private readonly IBlobStorageService _blobStorageService;

    public BlobUploadFunction(ILogger<BlobUploadFunction> logger, IBlobStorageService blobStorageService)
    {
        _logger = logger;
        _blobStorageService = blobStorageService;
    }

    [FunctionName("Upload")]
    public async Task<IActionResult> Upload([HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req)
    {
        string blobName = null, sasURI = null;
        try
        {
            var file = req.Form.Files["File"];
            blobName = $"{Guid.NewGuid()}-{file.FileName}";
            using (var ms = new MemoryStream())
            {
                file.CopyTo(ms); 
                await _blobStorageService.UploadFile(containerName: Constants.ImageContainer,
                                              fileName: blobName,
                                              contentType: file.ContentType,
                                              stream: ms);
            }

            sasURI = _blobStorageService.GetSharedAccessSignatureURL(blobName, Constants.ImageContainer, DateTime.UtcNow.AddHours(3));

        }
        catch (Exception ex)
        {
            _logger.LogError(ex.Message);
            _logger.LogError(ex.StackTrace);
            throw;
        }


        return new OkObjectResult(new
        {
            success = true,
            container = Constants.ImageContainer,
            blobName,
            sasURI
        });
    }
}
