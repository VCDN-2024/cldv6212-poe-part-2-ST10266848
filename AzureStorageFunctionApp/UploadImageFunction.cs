using Azure.Data.Tables;
using Azure;
using Azure.Storage.Blobs;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using System.ComponentModel.DataAnnotations;

namespace AzureStorageFunctionApp
{
    //Code Attribution for the below Function:
    //Author: Open AI
    //ChatBot Used: ChatGPT
    //Chat Link: https://chatgpt.com/share/66fa8244-50a4-8002-97b0-197a9b3660a7
    //Date Accessed: 30 September 2024

    public static class UploadImageFunction
    {
        [Function("UploadImageFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("UploadImageFunction");
            logger.LogInformation("LOG_INFO: Processing request to upload a product image to Azure Blob Storage...\n");

            try
            {
                if (!req.HasFormContentType)
                {
                    return new BadRequestObjectResult("Unsupported media type.\n");
                }

                var form = await req.ReadFormAsync();
                var file = form.Files["file"];


                if (file == null || file.Length == 0)
                {
                    return new BadRequestObjectResult("No file uploaded.\n");
                }

                //setting Blob Storage elements
                string fileName = file.FileName;
                string containerName = "products";
                string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

                using var stream = file.OpenReadStream();

                BlobServiceClient blobServiceClient = new BlobServiceClient(connectionString);

                BlobContainerClient containerClient = blobServiceClient.GetBlobContainerClient(containerName);

                await containerClient.CreateIfNotExistsAsync();

                BlobClient blobClient = containerClient.GetBlobClient(fileName);

                await blobClient.UploadAsync(stream, overwrite: true);

                //Return the blob URL
                var blobUri = blobClient.Uri.ToString();
                
                logger.LogInformation($"LOG: File '{fileName}' uploaded successfully to {blobUri}!\n");

                return new OkObjectResult(new { blobUrl = blobUri });
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "ERROR: An error occured during file upload.\n");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            } 
        }
    }
}
