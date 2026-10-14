using System.Collections.Concurrent;
using System.Net.Http.Headers;
using System.Security.Cryptography;
using System.Text.Json.Serialization;

namespace Shiny.DeviceTests.Runner.Fixtures;


/// <summary>
/// /files/* - deterministic downloads, upload capture and failure endpoints for the HttpTransfers suite.
/// Download bodies follow FilePattern so the app can verify them by hash without the runner sending one.
/// Every request carrying ?id= is recorded so a test can assert the method, headers and Range it sent.
/// </summary>
public static class HttpTransferFixture
{
    static readonly ConcurrentDictionary<string, List<RecordedRequest>> requests = new();
    static readonly ConcurrentDictionary<string, UploadRecord> uploads = new();
    static readonly ConcurrentDictionary<string, int> flakyHits = new();


    public static void MapHttpTransfer(this WebApplication app, RunnerState state)
    {
        // ?id= records the request, ?chunk=&delayMs= throttle the body (progress + cancel tests)
        app.MapMethods("/files/download/{bytes:long}", ["GET", "POST"], (HttpContext ctx, long bytes) => ServeAsync(ctx, bytes, null));

        // the first request for an ?id= drops the connection after dropAfter bytes; later ones serve normally (with Range)
        app.MapGet("/files/flaky/{bytes:long}", (HttpContext ctx, long bytes) =>
        {
            var id = ctx.Request.Query["id"].ToString();
            var dropAfter = Int64.TryParse(ctx.Request.Query["dropAfter"], out var d) ? d : bytes / 2;
            var hit = flakyHits.AddOrUpdate(id, 1, (_, x) => x + 1);
            return ServeAsync(ctx, bytes, hit == 1 ? dropAfter : null);
        });

        app.MapGet("/files/requests/{id}", (string id) => Results.Json(
            requests.TryGetValue(id, out var list) ? Snapshot(list) : [],
            HttpFixtureJsonContext.Default.ListRecordedRequest
        ));

        app.MapMethods("/files/upload/{id}", ["POST", "PUT"], async (HttpContext ctx, string id) =>
        {
            Record(ctx, id);
            var record = await ReadUploadAsync(ctx, id);
            uploads.AddOrUpdate(id, record, (_, existing) => record with { Count = existing.Count + 1 });
            return Results.Ok();
        });

        app.MapGet("/files/upload/{id}", (string id) => uploads.TryGetValue(id, out var record)
            ? Results.Json(record, HttpFixtureJsonContext.Default.UploadRecord)
            : Results.NotFound());

        // any method - answers with the given status code (the delegate's error path)
        app.MapMethods("/files/status/{code:int}", ["GET", "POST", "PUT"], async (HttpContext ctx, int code) =>
        {
            Record(ctx, ctx.Request.Query["id"].ToString());

            // drain an upload body so the client sees the status rather than a reset
            await ctx.Request.Body.CopyToAsync(Stream.Null);
            return Results.Text($"status {code} from the device test runner", statusCode: code);
        });

        app.MapDelete("/files", () =>
        {
            requests.Clear();
            uploads.Clear();
            flakyHits.Clear();
            return Results.Ok();
        });
    }


    static async Task ServeAsync(HttpContext ctx, long bytes, long? dropAfter)
    {
        Record(ctx, ctx.Request.Query["id"].ToString());

        var chunk = Int32.TryParse(ctx.Request.Query["chunk"], out var c) && c > 0 ? c : 64 * 1024;
        var delayMs = Int32.TryParse(ctx.Request.Query["delayMs"], out var dm) ? dm : 0;

        long start = 0;
        var range = ctx.Request.GetTypedHeaders().Range;
        var from = range?.Ranges.FirstOrDefault()?.From;
        if (from != null)
        {
            if (from.Value >= bytes)
            {
                ctx.Response.StatusCode = StatusCodes.Status416RangeNotSatisfiable;
                ctx.Response.Headers.ContentRange = $"bytes */{bytes}";
                return;
            }
            start = from.Value;
            ctx.Response.StatusCode = StatusCodes.Status206PartialContent;
            ctx.Response.Headers.ContentRange = new ContentRangeHeaderValue(start, bytes - 1, bytes).ToString();
        }

        ctx.Response.ContentType = "application/octet-stream";
        ctx.Response.Headers.AcceptRanges = "bytes";
        ctx.Response.ContentLength = bytes - start;

        var buffer = new byte[chunk];
        var position = start;
        while (position < bytes)
        {
            var count = (int)Math.Min(chunk, bytes - position);
            if (dropAfter != null && position + count > dropAfter.Value)
            {
                count = (int)Math.Max(0, dropAfter.Value - position);
                FilePattern.Fill(buffer.AsSpan(0, count), position);
                await ctx.Response.Body.WriteAsync(buffer.AsMemory(0, count), ctx.RequestAborted);
                await ctx.Response.Body.FlushAsync(ctx.RequestAborted);
                ctx.Abort();
                return;
            }

            FilePattern.Fill(buffer.AsSpan(0, count), position);
            await ctx.Response.Body.WriteAsync(buffer.AsMemory(0, count), ctx.RequestAborted);
            position += count;

            if (delayMs > 0)
            {
                await ctx.Response.Body.FlushAsync(ctx.RequestAborted);
                await Task.Delay(delayMs, ctx.RequestAborted);
            }
        }
    }


    static async Task<UploadRecord> ReadUploadAsync(HttpContext ctx, string id)
    {
        var headers = Headers(ctx.Request);
        var contentType = ctx.Request.ContentType ?? String.Empty;

        if (ctx.Request.HasFormContentType && contentType.StartsWith("multipart/", StringComparison.OrdinalIgnoreCase))
        {
            var form = await ctx.Request.ReadFormAsync(ctx.RequestAborted);
            var file = form.Files.FirstOrDefault();
            var fields = form.ToDictionary(x => x.Key, x => x.Value.ToString());

            long length = 0;
            var sha = String.Empty;
            if (file != null)
            {
                await using var stream = file.OpenReadStream();
                (length, sha) = await HashAsync(stream, ctx.RequestAborted);
            }
            return new UploadRecord(id, ctx.Request.Method, contentType, length, sha, file?.FileName, file?.Name, fields, headers, 1);
        }

        var (rawLength, rawSha) = await HashAsync(ctx.Request.Body, ctx.RequestAborted);
        return new UploadRecord(id, ctx.Request.Method, contentType, rawLength, rawSha, null, null, [], headers, 1);
    }


    static async Task<(long Length, string Sha256)> HashAsync(Stream stream, CancellationToken ct)
    {
        using var sha = IncrementalHash.CreateHash(HashAlgorithmName.SHA256);
        var buffer = new byte[81920];
        long length = 0;
        int read;
        while ((read = await stream.ReadAsync(buffer, ct)) > 0)
        {
            sha.AppendData(buffer, 0, read);
            length += read;
        }
        return (length, Convert.ToHexString(sha.GetHashAndReset()));
    }


    static void Record(HttpContext ctx, string id)
    {
        if (String.IsNullOrWhiteSpace(id))
            return;

        var list = requests.GetOrAdd(id, _ => []);
        lock (list)
            list.Add(new RecordedRequest(ctx.Request.Method, ctx.Request.Headers.Range.ToString(), Headers(ctx.Request)));
    }


    static List<RecordedRequest> Snapshot(List<RecordedRequest> list)
    {
        lock (list)
            return [.. list];
    }


    static Dictionary<string, string> Headers(HttpRequest request) => request
        .Headers
        .ToDictionary(x => x.Key, x => x.Value.ToString(), StringComparer.OrdinalIgnoreCase);
}


// byte i of every download = (i * 31 + 7) mod 251 - mirrored by the app to verify content
public static class FilePattern
{
    public static void Fill(Span<byte> buffer, long offset)
    {
        for (var i = 0; i < buffer.Length; i++)
            buffer[i] = (byte)(((offset + i) * 31 + 7) % 251);
    }
}


public record RecordedRequest(string Method, string? Range, Dictionary<string, string> Headers);

public record UploadRecord(
    string Id,
    string Method,
    string ContentType,
    long Length,
    string Sha256,
    string? FileName,
    string? FileFieldName,
    Dictionary<string, string> Form,
    Dictionary<string, string> Headers,
    int Count
);


[JsonSourceGenerationOptions(PropertyNamingPolicy = JsonKnownNamingPolicy.CamelCase)]
[JsonSerializable(typeof(List<RecordedRequest>))]
[JsonSerializable(typeof(UploadRecord))]
public partial class HttpFixtureJsonContext : JsonSerializerContext;
