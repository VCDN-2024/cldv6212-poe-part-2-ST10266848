using Azure;
using Azure.Core;
using Azure.Data.Tables;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Http;
using Microsoft.AspNetCore.Mvc;
using Microsoft.Azure.Functions.Worker;
using Microsoft.Extensions.Logging;
using Newtonsoft.Json;
using System.ComponentModel.DataAnnotations;

namespace AzureStorageFunctionApp
{
    //Code Attribution for the below Function:
    //Author: Open AI
    //ChatBot Used: ChatGPT
    //Chat Link: https://chatgpt.com/share/66fa8244-50a4-8002-97b0-197a9b3660a7
    //Date Accessed: 30 September 2024

    public static class SendOrderNotificationFunction
    {
        [Function("SendOrderNotificationFunction")]
        public static async Task<IActionResult> Run(
            [HttpTrigger(AuthorizationLevel.Function, "post", Route = null)] HttpRequest req,
            FunctionContext executionContext)
        {
            var logger = executionContext.GetLogger("SendOrderNotificationFunction");

            logger.LogInformation("LOG_INFO: Processing order notification request...\n");

            //Read request body
           var requestBody = await new StreamReader(req.Body).ReadToEndAsync();
           var order = JsonConvert.DeserializeObject<Order>(requestBody);

            if (order == null)
            {
                return new BadRequestObjectResult("Invalid order data.\n");
            }

            // Define the Azure Storage connection string
            string connectionString = Environment.GetEnvironmentVariable("AzureWebJobsStorage");

            // Initializing the TableClient for the "Products" table
            var _queueClient = new QueueClient(connectionString, "notifications");

            // Create notification message
            string notification = $"New order by User {order.UserId} for Product {order.ProductId} on {order.OrderDate}";
            logger.LogInformation($"LOG_INFO: Sending notification: {notification}\n");

            try
            {
                // Send message to the queue
                await _queueClient.SendMessageAsync(notification);
                logger.LogInformation("LOG_INFO: Notification sent successfully!\n");
            }
            catch (Exception ex)
            {
                logger.LogError($"LOG_ERROR: Failed to send notification: {ex.Message}\n");
                return new StatusCodeResult(StatusCodes.Status500InternalServerError);
            }
            return new OkObjectResult($"Notification sent: {notification}\n");
        }
    }

    public class Order : ITableEntity
    {
        [Key]
        public string OrderId { get; set; }
        public DateTime OrderDate { get; set; }

        //Simple validation:
        //Introduce required attribute and stringlength validation for each of the below

        [Required(ErrorMessage = "Please enter the Credit Card Name!")]
        public string CreditCardName { get; set; } = null!; //no real specific length honestly

        [Required(ErrorMessage = "Please enter the Credit Card Number!")]
        public string CreditCardNumber { get; set; } = null!; //certain length - about 16

        [Required(ErrorMessage = "Please enter the Credit Card Expiry Date!")]
        public string CreditCardExpDate { get; set; } = null!; //certain length - 

        [Required(ErrorMessage = "Please enter the Credit Card CCV Code!")]
        public string CreditCardCCVcode { get; set; } = null!; //cetain length - about 7

        [Required(ErrorMessage = "Please enter a shipping address!")]
        public string ShippingAddress { get; set; } = null!;

        //FK's:
        [Required(ErrorMessage = "Please select a User!")]
        public string UserId { get; set; } = null!; // FK to the User who placed the order

        [Required(ErrorMessage = "Please select a Product!")]
        public string ProductId { get; set; } // FK to the Product being ordered

        public string PartitionKey { get; set; } = null!;

        public string RowKey { get; set; } = null!;
        public DateTimeOffset? Timestamp { get; set; }
        public ETag ETag { get; set; }
    }

}
