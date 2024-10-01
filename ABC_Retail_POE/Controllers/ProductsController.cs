using ABC_Retail_POE.Models;
using ABC_Retail_POE.Services;
using Microsoft.AspNetCore.Mvc;
using System.Text;
using Newtonsoft.Json;
using static System.Net.WebRequestMethods;

namespace ABC_Retail_POE.Controllers
{
    public class ProductsController : Controller
    {
        private readonly BlobService _blobService;
        private readonly TableStorageService _tableStorageService;
        private readonly HttpClient _httpClient;

        public ProductsController(BlobService blobService, TableStorageService tableStorageService, HttpClient httpClient)
        {
            _blobService = blobService;
            _tableStorageService = tableStorageService;
            _httpClient = httpClient;

            
        }

        [HttpGet]
        public IActionResult AddProduct()
        {
            return View();
        }

        //Code Attribution for calling the AddProduct and UploadImage functions
        //and passing data:
        //Author: Open AI
        //ChatBot Used: ChatGPT
        //Chat Link: https://chatgpt.com/share/66fa8244-50a4-8002-97b0-197a9b3660a7
        //Date Accessed: 30 September 2024

        [HttpPost]
        public async Task<IActionResult> AddProduct(Product product, IFormFile file)
        {
            if (file != null)
            {
                //Using HttpClient to call the UploadImageFunction
                var formData = new MultipartFormDataContent();
                using (var memoryStream = new MemoryStream())
                {
                    await file.CopyToAsync(memoryStream);

                    //Reset the stream position for reading
                    memoryStream.Position = 0; 

                    var streamContent = new StreamContent(memoryStream);
                    formData.Add(streamContent, "file", file.FileName);

                    //Set the URL of your Azure Function
                    string uploadImageFunctionUrl = "http://localhost:7284/api/UploadImageFunction";

                    //Call the UploadImage function (by sending an HTTP POST request to the function)
                    var response = await _httpClient.PostAsync(uploadImageFunctionUrl, formData);

                    if (response.IsSuccessStatusCode)
                    {
                        var responseContent = await response.Content.ReadAsStringAsync();

                        Console.WriteLine($"Response from UploadImageFunction: {responseContent}");

                        //Ensure the response is correctly deserialized
                        var responseObject = JsonConvert.DeserializeObject<dynamic>(responseContent);

                        //Now extract the Blob URL and assign it to the product
                        if (responseObject != null && responseObject.blobUrl != null)
                        {
                            product.ImageUrl = responseObject.blobUrl.ToString();
                            //Log to verify if ImageUrl is set correctly
                            Console.WriteLine($"ImageUrl set to: {product.ImageUrl}");
                        }
                        else
                        {
                            Console.WriteLine("Failed to set ImageUrl from the UploadImageFunction response.");
                        }
                    }
                    else
                    {
                        ModelState.AddModelError(string.Empty, "Error uploading image. Please try again.");
                    }
                }
            }

            if (!ModelState.IsValid)
            {
                product.PartitionKey = "ProductsPartition";
                product.RowKey = Guid.NewGuid().ToString();
                product.ProductId = product.RowKey;

                //Code Attribution for the below call to the function and necessary code:
                //Author: Open AI
                //ChatBot Used: ChatGPT
                //Chat Link: https://chatgpt.com/share/66fa8244-50a4-8002-97b0-197a9b3660a7
                //Date Accessed: 28 September 2024

                // Serialize product object to JSON
                var productJson = JsonConvert.SerializeObject(product);
                Console.WriteLine($"Serialized Product JSON: {productJson}");

                var jsonContent = new StringContent(productJson, Encoding.UTF8, "application/json");

                // Set the URL of your Azure Function
                //string addProductFunctionUrl = "https://<your-function-app-name>.azurewebsites.net/api/AddProduct";
                string addProductFunctionUrl = "http://localhost:7284/api/AddProductFunction";


                //Call the AddProduct function (by sending a HTTP POST request to the function)
                var response = await _httpClient.PostAsync(addProductFunctionUrl, jsonContent);

                //Check if the request was successful
                if (response.IsSuccessStatusCode)
                {
                    return RedirectToAction("Index");
                }
                else
                {
                    // Handle the error case (log, show an error message, etc.)
                    ModelState.AddModelError(string.Empty, "Error adding product. Please try again.");
                }
                //return RedirectToAction("Index");
            }
            return View(product);
        }

        [HttpPost]
        public async Task<IActionResult> DeleteProduct(string partitionKey, string rowKey, Product product)
        {

            if (product != null && !string.IsNullOrEmpty(product.ImageUrl))
            {
                //Delete the associated blob image
                await _blobService.DeleteBlobAsync(product.ImageUrl);
            }
            //Delete Table entity
            await _tableStorageService.DeleteProductAsync(partitionKey, rowKey);

            return RedirectToAction("Index");
        }

        public async Task<IActionResult> Index()
        {
            var products = await _tableStorageService.GetAllProductsAsync();
            return View(products);
        }
    }
}
