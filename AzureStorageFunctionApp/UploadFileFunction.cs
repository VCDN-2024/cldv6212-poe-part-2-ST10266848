using Azure.Storage.Files.Shares;
using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;

namespace AzureStorageFunctionApp
{
    public static class UploadFileFunction
    {
        //Code Attribution for the below Function:
        //Author: Open AI
        //ChatBot Used: ChatGPT
        //Chat Link: https://chatgpt.com/share/66fa8244-50a4-8002-97b0-197a9b3660a7
        //Date Accessed: 30 September 2024

        [Function("UploadFileFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("UploadFileFunction");
            logger.LogInformation("\nLOG_INFO: Processing file upload request...\n");

            // Read file from request
            var formCollection = await req.ReadFormAsync();
            var file = formCollection.Files.FirstOrDefault();

            if (file == null || file.Length == 0)
            {
                return new BadRequestObjectResult("No file uploaded.\n");
            }

            // Define the Azure Storage connection string and file share name
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");
            string fileShareName = "myproducts";
            string directoryName = "uploads";

            try
            {
                var shareClient = new ShareServiceClient(connectionString);
                var share = shareClient.GetShareClient(fileShareName);
                var directory = share.GetDirectoryClient(directoryName);

                // Create directory if it doesn't exist
                await directory.CreateIfNotExistsAsync();

                // Get a reference to the file client
                var fileClient = directory.GetFileClient(file.FileName);

                // Upload the file to Azure File Share
                using (var stream = file.OpenReadStream())
                {
                    await fileClient.CreateAsync(stream.Length);
                    await fileClient.UploadRangeAsync(new HttpRange(0, stream.Length), stream);
                }

                logger.LogInformation($"LOG_INFO: File '{file.FileName}' uploaded successfully to '{directoryName}' in file share '{fileShareName}'!\n");
                return new OkObjectResult($"LOG_INFO: File '{file.FileName}' uploaded successfully!\n");
            }
            catch (Exception ex)
            {
                logger.LogError($"LOG_ERROR: Error uploading file: {ex.Message}\n");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }

        }
    }


}
