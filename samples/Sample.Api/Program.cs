using System.Text.Json;
using System.Text.Json.Serialization;
using Microsoft.AspNetCore.Http.Features;
using Sample.Api;
using Shiny.DocumentDb;
using Shiny.DocumentDb.Sqlite;

var builder = WebApplication.CreateBuilder(args);

var uploadRoot = Path.Combine(builder.Environment.ContentRootPath, "uploads");
Directory.CreateDirectory(uploadRoot);
var dbPath = Path.Combine(builder.Environment.ContentRootPath, "app.db");

builder.Services.AddDocumentStore(opts =>
{
    opts.DatabaseProvider = new SqliteDatabaseProvider($"Data Source={dbPath}");
    opts.JsonSerializerOptions = new JsonSerializerOptions
    {
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        Converters = { new JsonStringEnumConverter() }
    };
    opts
        .MapTypeToTable<TodoItem>("todos", t => t.Identifier)
        .MapTypeToTable<SyncChange>("sync_changes")
        .MapTypeToTable<FileRecord>("files");
});

builder.Services.ConfigureHttpJsonOptions(o =>
{
    o.SerializerOptions.PropertyNamingPolicy = JsonNamingPolicy.CamelCase;
    o.SerializerOptions.Converters.Add(new JsonStringEnumConverter());
});

// Allow large request bodies (multi-GB uploads) - default is ~28 MB.
builder.Services.Configure<FormOptions>(o =>
{
    o.MultipartBodyLengthLimit = long.MaxValue;
    o.ValueLengthLimit = int.MaxValue;
});
builder.WebHost.ConfigureKestrel(o => o.Limits.MaxRequestBodySize = null);

builder.Services.AddProblemDetails();

var app = builder.Build();

app.UseExceptionHandler();
app.UseStatusCodePages();

app.MapGet("/", () => Results.Json(new
{
    name = "Shiny Sample API",
    description = "Endpoints for Shiny.Net.Http (large file transfers) and Shiny.Data.Sync (delta sync).",
    endpoints = new
    {
        files = new
        {
            list = "GET /files",
            upload_raw = "POST /files/upload?name={fileName}  (body = raw bytes, returns FileRecord)",
            upload_multipart = "POST /files/upload  (multipart/form-data with file field 'file')",
            download = "GET /files/{id}  (supports Range)",
            metadata = "GET /files/{id}/info",
            delete = "DELETE /files/{id}"
        },
        sync = new
        {
            list_current = "GET /todos/current  (all undeleted TodoItems)",
            pull_delta = "GET /todos?since={cursor}&excludeDeletes={bool}  (Shiny.Data.Sync inbox shape)",
            tombstones = "GET /todos/tombstones?since={cursor}  (Shiny.Data.Sync tombstone endpoint shape)",
            create = "POST /todos  (Shiny.Data.Sync outbox: Create)",
            update = "PUT /todos/{id}  (Shiny.Data.Sync outbox: Update)",
            delete = "DELETE /todos/{id}  (Shiny.Data.Sync outbox: Delete)",
            batch = "POST /todos/batch  (Shiny.Data.Sync batched outbox)"
        }
    }
}));

FileEndpoints.Map(app, uploadRoot);
TodoSyncEndpoints.Map(app);

app.Run();
