using System.Drawing;

namespace Sample.Api.Controllers;


[ApiController]
[Route("[controller]")]
public class TransfersController : ControllerBase
{
    const string TEST_DOWNLOAD_URI = "http://ipv4.download.thinkbroadband.com/{0}MB.zip";
    static readonly HttpClient httpClient = new();
    readonly ILogger logger;


    public TransfersController(ILogger<TransfersController> logger)
    {
        this.logger = logger;
    }


    [HttpGet("download")]
    public Task<IActionResult> Get()
        => this.GetDownload(50);


    [HttpGet("error")]
    [HttpPost("error")]
    public IActionResult RequestError()
        => this.BadRequest();    

    [HttpPost("download/body")]
    public Task<IActionResult> DownloadWithBody([FromBody] BodyPackage package)
    {
        this.logger.LogInformation("Package Text: " + package?.Text);
        if (!Int32.TryParse(package?.Text, out var size))
            size = 50;

        return this.GetDownload(size);
    }


    [HttpPost("upload")]
    public async Task<IActionResult> Upload([FromForm] IFormFile file)
    {
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


    async Task<IActionResult> GetDownload(int size)
    {
        this.IterateHeaders();
        switch (size)
        {
            case 5:
            case 50:
            case 512: break;
            default: throw new InvalidOperationException("Invalid Size");
        }
        var uri = String.Format(TEST_DOWNLOAD_URI, size);
        var fn = String.Format("{0}MB.zip", size);
        var stream = await httpClient.GetStreamAsync(uri);

        return this.File(stream, fn, true);
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