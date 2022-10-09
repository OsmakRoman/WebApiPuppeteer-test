using Microsoft.AspNetCore.Mvc;
using PuppeteerSharp;
using System.Net.Mime;
using System.Security.Cryptography;
using static System.Net.Mime.MediaTypeNames;


namespace WebApiPuppeteer.Controllers
{
    [ApiController]
    [Route("[controller]")]
    public class ConverterController : ControllerBase
    {
       
        private readonly Microsoft.AspNetCore.Hosting.IWebHostEnvironment _env;

        public ConverterController(ILogger<ConverterController> logger, Microsoft.AspNetCore.Hosting.IWebHostEnvironment env)
        {
            _env = env;
        }

        [HttpGet("{id}")]
        public IActionResult Download(string id)
        {
            var path = _env.ContentRootPath;
            var fileReadDirectory = Path.Combine(path, "Files");

            foreach (string f in Directory.GetFiles(fileReadDirectory))
            {
                if (Path.GetFileNameWithoutExtension(f).Contains(id))
                {

                    var bytes = System.IO.File.ReadAllBytes(f);
                    System.IO.File.Delete(f);
                    return File(bytes, "text/html", Path.GetFileName(f).Remove(0, 37));
                    
                }               
            }
     
            return Content(id);
        }

        [HttpPost]
        public async Task<IActionResult> Upload()
        {
            var file = HttpContext.Request.Form.Files[0];
            var fileType = Path.GetExtension(file.FileName);
            var fileName = Path.GetFileNameWithoutExtension(file.FileName);
            var filePath = _env.ContentRootPath;

            Guid id = Guid.NewGuid();
            var htmlFullFileName = Path.Combine(filePath, "Files", id + "." + fileName + ".html");
            var pdfFullFileName = Path.Combine(filePath, "Files", id + "." + fileName + ".pdf");

            if (fileType.ToLower() == ".html")
            {
                if (file != null && file.Length > 0)
                {
                    using (var stream = new FileStream(htmlFullFileName, FileMode.Create))
                    {
                        await file.CopyToAsync(stream);
                    }

                    await new BrowserFetcher().DownloadAsync(BrowserFetcher.DefaultChromiumRevision);
                    var browser = await Puppeteer.LaunchAsync(new LaunchOptions
                    {
                        Headless = true
                    });
                    var page = await browser.NewPageAsync();
                    string markup = System.IO.File.ReadAllText(htmlFullFileName);                
                    await page.SetContentAsync(markup);
                    await page.PdfAsync(pdfFullFileName);
                    await browser.CloseAsync();

                    System.IO.File.Delete(htmlFullFileName);

                    return Content(id.ToString());
                }
                else
                {
                    return Content("File empty");
                }
            }

            return Content("File is not html");
        }

    }
}