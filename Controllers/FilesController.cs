using System;
using System.Collections.Generic;
using Azure.DTO;
using Azure.Storage.Blobs;
using Azure.Storage.Blobs.Models;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Extensions.Configuration;

namespace Azure.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class FilesController : ControllerBase
    {
        private readonly string _connectionString;
        private readonly string _containerName;

        public FilesController(IConfiguration configuration)
        {
            _connectionString = configuration.GetValue<string>("BlobConnectionString");
            _containerName = configuration.GetValue<string>("BlobContainerName");
        }

        [HttpPost("Upload")]
        public IActionResult UploadFile(IFormFile file) 
        {
            BlobContainerClient container = new(_connectionString, _containerName);
            BlobClient blob = container.GetBlobClient(file.FileName);

            using var data = file.OpenReadStream();
            blob.Upload(data, new BlobUploadOptions
            {
                HttpHeaders = new BlobHttpHeaders { ContentType = file.ContentType }
            });

            return Ok(blob.Uri.ToString());
        }

        [HttpGet("Download/{name}")]
        public IActionResult DownloadFile(string name)
        {
            BlobContainerClient container = new(_connectionString, _containerName);
            BlobClient blob = container.GetBlobClient(name);

            if (!blob.Exists()) return BadRequest();

            var result = blob.DownloadContent();
            return File(result.Value.Content.ToArray(), result.Value.Details.ContentType, blob.Name);
        }

        [HttpDelete("Delete/{name}")]
        public IActionResult DeleteFile(string name)
        {
            BlobContainerClient container = new(_connectionString, _containerName);
            BlobClient blob = container.GetBlobClient(name);

            blob.DeleteIfExists();
            return NoContent();
        }

        [HttpGet("GetAllFiles")]
        public IActionResult GetAllFiles()
        {
            var blobsDto = new List<BlobDto>(); 

            BlobContainerClient container = new(_connectionString, _containerName);

            foreach (var blob in container.GetBlobs())
            {
                blobsDto.Add(new BlobDto
                {
                    Name = blob.Name,
                    Type = blob.Properties.ContentType,
                    Uri = container.Uri.AbsoluteUri + "/" + blob.Name
                });
            }

            return Ok(blobsDto);
        }
    }
}