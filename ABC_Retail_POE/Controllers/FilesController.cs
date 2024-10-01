using ABC_Retail_POE.Models;
using Microsoft.AspNetCore.Mvc;

namespace ABC_Retail_POE.Controllers
{
    public class FilesController : Controller
    {
        private readonly AzureFileShareService _fileShareService;

        public FilesController(AzureFileShareService fileShareService, HttpClient httpClient)
        {
            _fileShareService = fileShareService;  
        }

        public async Task<IActionResult> Index()
        {
            List<FileModel> files;
            try
            {
                files = await _fileShareService.ListFilesAsync("uploads");
            }
            catch (Exception ex)
            {
                ViewBag.Message = $"Failed to load files: {ex.Message}";
                files = new List<FileModel>();
            }

            return View(files);
        }

        [HttpPost]
        public async Task<IActionResult> Upload(IFormFile file)
        {
            if (file == null || file.Length == 0)
            {
                ModelState.AddModelError("file", "Please select a file to upload.");
                return await Index();
            }

            try
            {
                //Prepare the HTTP client
               using var client = new HttpClient();

                //Create a multipart form data content
                var content = new MultipartFormDataContent();
                var fileStream = file.OpenReadStream();
                content.Add(new StreamContent(fileStream), "file", file.FileName);

                //Call the Azure Function
                var response = await client.PostAsync("http://localhost:7284/api/UploadFileFunction", content);

                if (response.IsSuccessStatusCode)
                {
                    TempData["Message"] = $"File '{file.FileName}' uploaded successfully!";
                }
                else
                {
                    TempData["Message"] = $"File upload failed: {await response.Content.ReadAsStringAsync()}";
                }
            }
            catch (Exception ex)
            {
                TempData["Message"] = $"File upload failed: {ex.Message}";
            }

            return RedirectToAction("Index");
        }

        //Handle file download
        [HttpGet]
        public async Task<IActionResult> DownloadFile(string fileName)
        {
            if (string.IsNullOrEmpty(fileName))
            {
                return BadRequest("File name cannot be null or empty.");
            }

            try
            {
                var fileStream = await _fileShareService.DownloadFileAsync("uploads", fileName);

                if (fileStream == null)
                {
                    return NotFound($"File '{fileName}' not found.");
                }

                return File(fileStream, "application/octet-stream", fileName);
            }
            catch (Exception ex)
            {
                return BadRequest($"Error downloading file: {ex.Message}");
            }
        }
    }
}
