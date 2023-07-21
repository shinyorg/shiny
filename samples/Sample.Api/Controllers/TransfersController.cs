using System.Linq;

namespace Sample.Api.Controllers;


[ApiController]
[Route("[controller]")]
public class TransfersController : ControllerBase
{
    readonly ILogger logger;


    public TransfersController(ILogger<TransfersController> logger)
    {
        this.logger = logger;
    }


    [HttpGet("download")]
    public IActionResult Get()
        => this.GetDownload(50);


    [HttpGet("error")]
    [HttpPost("error")]
    public IActionResult RequestError()
        => this.BadRequest();


    [HttpPost("download/body")]
    public IActionResult DownloadWithBody([FromBody] BodyPackage package)
    {
        this.logger.LogInformation("Package Text: " + package?.Text);
        if (!Int32.TryParse(package?.Text, out var size))
            size = 50;

        return this.GetDownload(size);
    }


    [HttpPost("upload")]
    //public async Task<IActionResult> Upload()
    public async Task<IActionResult> Upload(IFormFile file)
    {
        //this.logger.LogInformation("Files Uploading: " + this.Request.Form.Files.Count);

        //await this.Write(this.Request.Form.Files.First());
        await this.Write(file);
        return this.Ok();
    }


    [HttpPost("upload/body")]
    public async Task<IActionResult> UploadWithBody([FromForm] BodyPackage package)
    {
        if (package.File == null)
            throw new InvalidOperationException("File Not Sent");

        this.logger.LogInformation("Package Text: " + package.Text);
        await this.Write(package.File!);
        return this.Ok();
    }


    IActionResult GetDownload(int size)
    {
        this.IterateHeaders();
        var file = this.GetTempFile(size);
        var fn = String.Format("{0}MB.zip", size);

        return this.PhysicalFile(file, "application/octet-stream", fn, true);
    }


    string GetTempFile(int sizeInMB)
    {
        var path = Path.GetTempFileName();
        var data = new byte[8192];
        var rng = new Random();

        using (var fs = System.IO.File.OpenWrite(path))
        {
            for (var i = 0; i < sizeInMB * 128; i++)
            {
                rng.NextBytes(data);
                fs.Write(data, 0, data.Length);
            }
            fs.Flush();
        }

        this.logger.LogInformation($"Upload File Generated - {new FileInfo(path).Length} bytes");
        return path;
    }


    void IterateHeaders()
    {
        foreach (var header in this.Request.Headers)
        {
            foreach (var value in header.Value)
            {
                this.logger.LogInformation($"HEADER: {header.Key} - VALUE: {value}");
            }
        }
    }


    async Task Write(IFormFile file)
    {
        this.IterateHeaders();

        var filePath = Path.Combine(Path.GetTempPath(), file.FileName);
        this.logger.LogInformation($"Writing to '{filePath}' with size of {file.Length} bytes");

        using (var fs = System.IO.File.Create(filePath))
            await file.CopyToAsync(fs);

        this.logger.LogInformation("Write Complete");
    }
}


public class BodyPackage
{
    public string? Text { get; set; }
    public IFormFile? File { get; set; }
}