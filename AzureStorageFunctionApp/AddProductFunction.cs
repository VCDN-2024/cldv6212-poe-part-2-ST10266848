using Azure.Data.Tables;
using Azure;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace AzureStorageFunctionApp
{
    public static class AddProductFunction
    {
        //Code Attribution for the below Function:
        //Author: Open AI
        //ChatBot Used: ChatGPT
        //Chat Link: https://chatgpt.com/share/66fa8244-50a4-8002-97b0-197a9b3660a7
        //Date Accessed: 28 September 2024


        [Function("AddProductFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            FunctionContext executionContext)
        {
            //log.LogInformation("Processing request to add a product to Azure Table Storage.");

            var logger = executionContext.GetLogger("AddProductFunction");
            logger.LogInformation("LOG_INFO: Processing request to add a product to Azure Table Storage...\n");

            // Read request body
            string requestBody = await new StreamReader(req.Body).ReadToEndAsync();

            // Deserialize to the existing Product class
            var product = JsonConvert.DeserializeObject<Product>(requestBody);

            // Log to verify ImageUrl after deserialization
            logger.LogInformation($"LOG_INFO: ImageUrl after deserialization: {product.ImageUrl}\n");

            // Validate input (e.g., ProductId and ProductName must be provided)
            if (string.IsNullOrEmpty(product.PartitionKey) || string.IsNullOrEmpty(product.RowKey))
            {
                return new BadRequestObjectResult("PartitionKey and RowKey must be set before adding a product!\n");
            }

            // Define the Azure Storage connection string
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            // Initializing the TableClient for the "Products" table
            var _tableClient = new TableClient(connectionString, "Products");

            // Ensure that the table exists
            await _tableClient.CreateIfNotExistsAsync();

            try
            {
                // Insert the product entity into the table
                await _tableClient.AddEntityAsync(product);
            }
            catch (RequestFailedException ex)
            {
                logger.LogError(ex, "LOG_ERROR: Error adding product to table storage.\n");
                // Handle exception as necessary, for example log it or rethrow
                throw new InvalidOperationException("Error adding entity to Table Storage", ex);
            }

            logger.LogInformation($"LOG: Product {product.ProductName} added successfully with ImageUrl: {product.ImageUrl}\n");

            return new OkObjectResult($"Product {product.ProductName} added successfully!\n");
        }
    }

    public class Product : ITableEntity
    {
        //Product Attributes:
        [Key]
        public string ProductId { get; set; } = null!;

        [Required(ErrorMessage = "Please select a product category!")]
        public string ProductCategory { get; set; } = null!;

        [Required(ErrorMessage = "Please enter the name of the product!")]
        public string ProductName { get; set; } = null!;

        [Required(ErrorMessage = "Please enter the description of the product!")]
        public string ProductDescription { get; set; } = null!;

        [Required(ErrorMessage = "Please enter the product's price!")]
        public double ProductPrice { get; set; }

        [Required(ErrorMessage = "Please upload a picture of the product!")]
        public string ImageUrl { get; set; } = null!;

        //ITableEntity implementation
        public string PartitionKey { get; set; } = null!;
        public string RowKey { get; set; } = null!;
        public ETag ETag { get; set; }
        public DateTimeOffset? Timestamp { get; set; }
    }

}
