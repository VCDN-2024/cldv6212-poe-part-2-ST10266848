using ABC_Retail_POE.Models;
using Azure.Storage.Queues;
using Microsoft.AspNetCore.Mvc;
using Newtonsoft.Json;
using System.Text;

namespace ABC_Retail_POE.Controllers
{
    public class OrdersController : Controller
    {
        private readonly TableStorageService _tableStorageService;
        private readonly QueueService _queueService;
        private readonly HttpClient _httpClient;
        

        public OrdersController(TableStorageService tableStorageService, QueueService queueService, HttpClient httpClient)
        {
            _tableStorageService = tableStorageService;
            _queueService = queueService;
            _httpClient = httpClient;
        }

        //Action to display all orders
        public async Task<IActionResult> Index()
        {
            var orders = await _tableStorageService.GetAllOrdersAsync();
            return View(orders);
        }
        public async Task<IActionResult> Create()
        {
            var users = await _tableStorageService.GetAllUsersAsync();
            var products = await _tableStorageService.GetAllProductsAsync();

            //Check for null or empty lists
            if (users == null || users.Count == 0)
            {
                //Handle the case where no users are found
                ModelState.AddModelError("", "No users found! Please add users first!");
                return View(); // Or redirect to another action
            }

            if (products == null || products.Count == 0)
            {
                //Handle the case where no birds are found
                ModelState.AddModelError("", "No products found! Please add products first!");
                return View(); // Or redirect to another action
            }

            ViewData["Users"] = users;
            ViewData["Products"] = products;

            return View();
        }


        //Code Attribution for calling the SendOrderNotification Function as well as sending
        //a message to the queue:
        //Author: Open AI
        //ChatBot Used: ChatGPT
        //Chat Link: https://chatgpt.com/share/66fa8244-50a4-8002-97b0-197a9b3660a7
        //Date Accessed: 30 September 2024

        //Action to handle the form submission and create the order
        [HttpPost]
        public async Task<IActionResult> Create(Order order)
        {
            if (!ModelState.IsValid)
            {//TableService
                order.OrderDate = DateTime.SpecifyKind(order.OrderDate, DateTimeKind.Utc);
                order.PartitionKey = "OrdersPartition";
                order.RowKey = Guid.NewGuid().ToString();
                order.OrderId = order.RowKey;

                await _tableStorageService.AddOrderAsync(order);

                //Calls the Azure Function to send the notification (SendOrderNotificationFunction)
                //which then sends a message to the queue
                string jsonOrder = JsonConvert.SerializeObject(order);
                var content = new StringContent(jsonOrder, Encoding.UTF8, "application/json");

                var response = await _httpClient.PostAsync("http://localhost:7284/api/SendOrderNotificationFunction", content);
                if (!response.IsSuccessStatusCode)
                {
                    //Handle error
                    ModelState.AddModelError("", "Failed to send notification.");
                }

                return RedirectToAction("Index");
            }
            else
            {
                //Log model state errors
                foreach (var error in ModelState)
                {
                    Console.WriteLine($"Key: {error.Key}, Errors: {string.Join(", ", error.Value.Errors.Select(e => e.ErrorMessage))}");
                }
            }

            //Reload users and products lists if validation fails
            var users = await _tableStorageService.GetAllUsersAsync();
            var products = await _tableStorageService.GetAllProductsAsync();
            ViewData["Users"] = users;
            ViewData["Products"] = products;

            return View(order);
        }

        public async Task<IActionResult> Delete(string partitionKey, string rowKey)
        {
            await _tableStorageService.DeleteOrderAsync(partitionKey, rowKey);
            return RedirectToAction("Index");
        }


    }
}
