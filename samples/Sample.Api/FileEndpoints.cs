using Shiny.DocumentDb;

namespace Sample.Api;


public static class FileEndpoints
{
    public static void Map(WebApplication app, string uploadRoot)
    {
        var group = app.MapGroup("/files");

        group.MapGet("/", async (IDocumentStore store) =>
            await store.Query<FileRecord>().OrderByDescending(f => f.CreatedAt).ToList());


        group.MapGet("/{id}/info", async (string id, IDocumentStore store) =>
        {
            var record = await store.Get<FileRecord>(id);
            return record is null ? Results.NotFound() : Results.Ok(record);
        });


        group.MapGet("/{id}", async (string id, IDocumentStore store) =>
        {
            var record = await store.Get<FileRecord>(id);
            if (record is null) return Results.NotFound();

            var path = Path.Combine(uploadRoot, id);
            if (!File.Exists(path)) return Results.NotFound();

            return Results.File(path, record.ContentType, record.FileName, enableRangeProcessing: true);
        });


        group.MapDelete("/{id}", async (string id, IDocumentStore store) =>
        {
            var record = await store.Get<FileRecord>(id);
            if (record is null) return Results.NotFound();

            var path = Path.Combine(uploadRoot, id);
            if (File.Exists(path))
                File.Delete(path);

            await store.Remove<FileRecord>(id);
            return Results.NoContent();
        });


        // POST /files/upload — supports both multipart/form-data AND raw body uploads.
        // Shiny.Net.Http's TransferType.UploadMultipart sends multipart; Upload sends raw.
        group.MapPost("/upload", async (HttpRequest req, IDocumentStore store, ILoggerFactory loggers) =>
        {
            var logger = loggers.CreateLogger("Sample.Api.Upload");
            var id = Guid.NewGuid().ToString("N");
            var storedPath = Path.Combine(uploadRoot, id);

            string fileName;
            string contentType;

            if (req.HasFormContentType)
            {
                var form = await req.ReadFormAsync();
                var file = form.Files.FirstOrDefault();
                if (file is null)
                    return Results.BadRequest(new { error = "No file form part found." });

                fileName = file.FileName;
                contentType = string.IsNullOrEmpty(file.ContentType) ? "application/octet-stream" : file.ContentType;

                await using var fs = File.Create(storedPath);
                await file.CopyToAsync(fs);
            }
            else
            {
                fileName = req.Query["name"].FirstOrDefault()
                    ?? req.Headers["X-File-Name"].FirstOrDefault()
                    ?? id;
                contentType = req.ContentType ?? "application/octet-stream";

                await using var fs = File.Create(storedPath);
                await req.Body.CopyToAsync(fs);
            }

            var size = new FileInfo(storedPath).Length;
            var baseUrl = $"{req.Scheme}://{req.Host}";
            var record = new FileRecord
            {
                Id = id,
                FileName = fileName,
                ContentType = contentType,
                SizeBytes = size,
                CreatedAt = DateTimeOffset.UtcNow,
                DownloadUrl = $"{baseUrl}/files/{id}"
            };
            await store.Insert(record);

            logger.LogInformation("Stored upload {id} ({size:N0} bytes) as {fileName}", id, size, fileName);
            return Results.Created(record.DownloadUrl, record);
        }).DisableAntiforgery();
    }
}
