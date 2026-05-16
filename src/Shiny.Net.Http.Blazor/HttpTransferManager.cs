using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text.Json.Serialization;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Microsoft.JSInterop;

namespace Shiny.Net.Http.Blazor;


/// <summary>
/// Blazor WASM implementation of <see cref="IHttpTransferManager"/> that persists
/// transfer requests to IndexedDB and drains them via the Service Worker
/// Background Sync API. Transfers continue running while the tab is closed
/// (where the browser supports background sync).
/// </summary>
public class HttpTransferManager(
    IJSRuntime jsRuntime,
    IServiceProvider services,
    BlazorHttpTransferOptions options,
    ILogger<HttpTransferManager> logger
) : IHttpTransferManager, IAsyncDisposable
{
    public event EventHandler<int>? CountChanged;
    public event EventHandler<HttpTransferResult>? UpdateReceived;

    IJSObjectReference? module;
    DotNetObjectReference<HttpTransferManager>? selfRef;
    bool initialized;


    // public void ComponentStart()
    // {
    //     // best-effort: warm up the SW registration on startup so the message
    //     // channel is wired immediately when the user later resolves the manager.
    //     _ = this.EnsureInit();
    // }


    async Task<IJSObjectReference> EnsureInit()
    {
        if (this.module == null)
        {
            this.module = await jsRuntime
                .InvokeAsync<IJSObjectReference>("import", "./_content/Shiny.Net.Http.Blazor/http-transfer.js")
                .ConfigureAwait(false);
        }

        if (!this.initialized)
        {
            this.selfRef ??= DotNetObjectReference.Create(this);
            this.initialized = await this.module
                .InvokeAsync<bool>("init", options.ServiceWorkerPath, this.selfRef)
                .ConfigureAwait(false);

            if (!this.initialized)
                throw new InvalidOperationException("Service Worker / IndexedDB are not supported in this browser");
        }
        return this.module;
    }


    public async Task<IList<HttpTransfer>> GetTransfers()
    {
        var mod = await this.EnsureInit().ConfigureAwait(false);
        var entries = await mod.InvokeAsync<JsTransferEntry[]>("getAll").ConfigureAwait(false);
        return entries.Select(ToTransfer).ToList();
    }


    public async Task<HttpTransfer> Queue(HttpTransferRequest request)
    {
        request.AssertValid();

        var mod = await this.EnsureInit().ConfigureAwait(false);

        var entry = new JsTransferEntry
        {
            Identifier = request.Identifier,
            Uri = request.Uri,
            Method = request.GetHttpMethod().Method,
            Type = request.Type == TransferType.Download ? "download" : "upload",
            Status = "pending",
            Headers = request.Headers != null ? new Dictionary<string, string>(request.Headers) : null,
            CreatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds(),
            UpdatedAt = DateTimeOffset.UtcNow.ToUnixTimeMilliseconds()
        };

        // For multipart uploads, project HttpContent / file path into a JS-friendly form.
        if (request.Type == TransferType.UploadMultipart)
        {
            var fileBytes = await ReadLocalFileAsync(request.LocalFilePath).ConfigureAwait(false);
            entry.FormData = new JsFormData
            {
                Fields = request.HttpContent != null
                    ? new Dictionary<string, string>
                    {
                        [request.HttpContent.ContentFormDataName ?? "data"] = request.HttpContent.Content
                    }
                    : null,
                File = new JsFormFile
                {
                    Name = request.FileFormDataName,
                    Filename = Path.GetFileName(request.LocalFilePath)
                }
            };
            await mod.InvokeVoidAsync("queue", entry).ConfigureAwait(false);
            await mod.InvokeVoidAsync(
                "setBody",
                request.Identifier,
                Convert.ToBase64String(fileBytes),
                "application/octet-stream"
            ).ConfigureAwait(false);
        }
        else if (request.Type == TransferType.UploadRaw)
        {
            var fileBytes = await ReadLocalFileAsync(request.LocalFilePath).ConfigureAwait(false);
            await mod.InvokeVoidAsync("queue", entry).ConfigureAwait(false);
            await mod.InvokeVoidAsync(
                "setBody",
                request.Identifier,
                Convert.ToBase64String(fileBytes),
                request.HttpContent?.ContentType ?? "application/octet-stream"
            ).ConfigureAwait(false);
        }
        else
        {
            await mod.InvokeVoidAsync("queue", entry).ConfigureAwait(false);
        }

        var transfer = new HttpTransfer(
            request,
            null,
            0,
            HttpTransferState.Pending,
            DateTimeOffset.UtcNow
        );
        await this.FireCountChanged().ConfigureAwait(false);
        return transfer;
    }


    public async Task Cancel(string identifier)
    {
        var mod = await this.EnsureInit().ConfigureAwait(false);
        await mod.InvokeVoidAsync("remove", identifier).ConfigureAwait(false);
        await this.FireCountChanged().ConfigureAwait(false);
    }


    public async Task CancelAll()
    {
        var mod = await this.EnsureInit().ConfigureAwait(false);
        await mod.InvokeVoidAsync("clear").ConfigureAwait(false);
        await this.FireCountChanged().ConfigureAwait(false);
    }


    async Task FireCountChanged()
    {
        try
        {
            var list = await this.GetTransfers().ConfigureAwait(false);
            this.CountChanged?.Invoke(this, list.Count);
        }
        catch (Exception ex)
        {
            logger.LogWarning(ex, "Failed to read transfer count for CountChanged");
        }
    }




    /// <summary>
    /// Reads a previously-completed download's bytes from IndexedDB. Returns null
    /// if the transfer is not yet complete or has no result blob.
    /// </summary>
    public async Task<byte[]?> GetDownloadBytes(string identifier)
    {
        var mod = await this.EnsureInit().ConfigureAwait(false);
        var b64 = await mod.InvokeAsync<string?>("getResultBase64", identifier).ConfigureAwait(false);
        return b64 == null ? null : Convert.FromBase64String(b64);
    }


    [JSInvokable]
    public Task OnStatusChanged(string identifier, string status)
    {
        var mapped = MapStatus(status);
        this.UpdateReceived?.Invoke(this, new HttpTransferResult(
            new HttpTransferRequest(identifier, "", TransferType.Download, ""),
            mapped,
            TransferProgress.Empty,
            null
        ));
        return Task.CompletedTask;
    }


    [JSInvokable]
    public async Task OnTransferCompleted(string identifier)
    {
        try
        {
            this.UpdateReceived?.Invoke(this, new HttpTransferResult(
                new HttpTransferRequest(identifier, "", TransferType.Download, ""),
                HttpTransferState.Completed,
                TransferProgress.Empty,
                null
            ));
            await this.FireCountChanged().ConfigureAwait(false);

            var entry = await this.GetEntry(identifier).ConfigureAwait(false);
            if (entry == null)
                return;

            var request = ToRequest(entry);
            await services
                .RunDelegates<IHttpTransferDelegate>(x => x.OnCompleted(request), logger)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error firing OnCompleted for {Identifier}", identifier);
        }
    }


    [JSInvokable]
    public async Task OnTransferError(string identifier, string message, int statusCode)
    {
        try
        {
            var ex = new HttpTransferException(message, statusCode);
            this.UpdateReceived?.Invoke(this, new HttpTransferResult(
                new HttpTransferRequest(identifier, "", TransferType.Download, ""),
                HttpTransferState.Error,
                TransferProgress.Empty,
                ex
            ));
            await this.FireCountChanged().ConfigureAwait(false);

            var entry = await this.GetEntry(identifier).ConfigureAwait(false);
            if (entry == null)
                return;

            var request = ToRequest(entry);
            await services
                .RunDelegates<IHttpTransferDelegate>(d => d.OnError(request, statusCode, ex), logger)
                .ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error firing OnError for {Identifier}", identifier);
        }
    }


    async Task<JsTransferEntry?> GetEntry(string identifier)
    {
        var mod = await this.EnsureInit().ConfigureAwait(false);
        return await mod.InvokeAsync<JsTransferEntry?>("get", identifier).ConfigureAwait(false);
    }


    static async Task<byte[]> ReadLocalFileAsync(string path)
    {
        // In Blazor WASM, LocalFilePath is generally a virtual path the host
        // application created (e.g. via File API or HttpClient download). We
        // read it through the standard managed File APIs which are mapped to
        // the WASM virtual file system.
        if (!File.Exists(path))
            throw new FileNotFoundException("Upload file does not exist", path);

        return await File.ReadAllBytesAsync(path).ConfigureAwait(false);
    }


    static HttpTransfer ToTransfer(JsTransferEntry e)
    {
        var req = ToRequest(e);
        return new HttpTransfer(
            req,
            e.BytesToTransfer,
            e.BytesTransferred,
            MapStatus(e.Status),
            DateTimeOffset.FromUnixTimeMilliseconds(e.CreatedAt)
        );
    }


    static HttpTransferRequest ToRequest(JsTransferEntry e)
    {
        var type = e.Type == "download" ? TransferType.Download : TransferType.UploadRaw;
        return new HttpTransferRequest(
            e.Identifier,
            e.Uri,
            type,
            e.Identifier, // we don't track a real local path on Blazor
            true,
            null,
            e.Headers
        )
        {
            HttpMethod = e.Method
        };
    }


    static HttpTransferState MapStatus(string s) => s switch
    {
        "pending" => HttpTransferState.Pending,
        "in-progress" => HttpTransferState.InProgress,
        "completed" => HttpTransferState.Completed,
        "error" => HttpTransferState.Error,
        _ => HttpTransferState.Unknown
    };


    public async ValueTask DisposeAsync()
    {
        this.selfRef?.Dispose();
        this.selfRef = null;

        if (this.module != null)
        {
            try { await this.module.DisposeAsync().ConfigureAwait(false); }
            catch { /* ignore */ }
            this.module = null;
        }
    }


    class JsTransferEntry
    {
        [JsonPropertyName("identifier")] public string Identifier { get; set; } = "";
        [JsonPropertyName("uri")] public string Uri { get; set; } = "";
        [JsonPropertyName("method")] public string Method { get; set; } = "GET";
        [JsonPropertyName("type")] public string Type { get; set; } = "download";
        [JsonPropertyName("status")] public string Status { get; set; } = "pending";
        [JsonPropertyName("headers")] public IDictionary<string, string>? Headers { get; set; }
        [JsonPropertyName("formData")] public JsFormData? FormData { get; set; }
        [JsonPropertyName("bytesTransferred")] public long BytesTransferred { get; set; }
        [JsonPropertyName("bytesToTransfer")] public long? BytesToTransfer { get; set; }
        [JsonPropertyName("createdAt")] public long CreatedAt { get; set; }
        [JsonPropertyName("updatedAt")] public long UpdatedAt { get; set; }
    }

    class JsFormData
    {
        [JsonPropertyName("fields")] public IDictionary<string, string>? Fields { get; set; }
        [JsonPropertyName("file")] public JsFormFile? File { get; set; }
    }

    class JsFormFile
    {
        [JsonPropertyName("name")] public string Name { get; set; } = "file";
        [JsonPropertyName("filename")] public string Filename { get; set; } = "file";
    }
}


public class HttpTransferException(string message, int statusCode) : Exception(message)
{
    public int StatusCode => statusCode;
}


