using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Text;
using System.Text.Json;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Data.Sync.Infrastructure;


/// <summary>
/// Default REST mapping for sync operations:
/// Create => POST {Url}, Update => PUT {Url}/{id}, Delete => DELETE {Url}/{id}.
/// Pull => GET {PullUrl ?? Url}?{CursorParameter}={cursor}.
/// Batch => POST {BatchUrl ?? Url + "/batch"}.
/// Tombstones => GET {TombstoneUrl}?{TombstoneCursorParameter}={cursor}.
/// </summary>
public class RestSyncTransport : ISyncTransport
{
    public const string HttpClientName = "Shiny.Data.Sync";

    readonly HttpClient http;
    readonly IReadOnlyList<ISyncInterceptor> interceptors;


    public RestSyncTransport(IHttpClientFactory factory, IEnumerable<ISyncInterceptor>? interceptors = null)
    {
        this.http = factory.CreateClient(HttpClientName);
        this.interceptors = interceptors?.ToList() ?? (IReadOnlyList<ISyncInterceptor>)Array.Empty<ISyncInterceptor>();
    }


    public async Task<SyncSendResult> Send(SyncEndpoint endpoint, SyncOperation operation, CancellationToken cancelToken)
    {
        var (method, uri) = operation.Verb switch
        {
            SyncVerb.Create => (HttpMethod.Post, endpoint.Url),
            SyncVerb.Update => (HttpMethod.Put, $"{endpoint.Url.TrimEnd('/')}/{Uri.EscapeDataString(operation.EntityIdentifier)}"),
            SyncVerb.Delete => (HttpMethod.Delete, $"{endpoint.Url.TrimEnd('/')}/{Uri.EscapeDataString(operation.EntityIdentifier)}"),
            _ => throw new InvalidOperationException("Unknown sync verb: " + operation.Verb)
        };

        using var req = new HttpRequestMessage(method, uri);
        if (operation.Payload != null && operation.Verb != SyncVerb.Delete)
            req.Content = new StringContent(operation.Payload, Encoding.UTF8, "application/json");

        await this.RunPushInterceptors(endpoint, new[] { operation }, req).ConfigureAwait(false);
        if (endpoint.OnBeforeSend != null)
            await endpoint.OnBeforeSend(req).ConfigureAwait(false);

        using var resp = await this.http.SendAsync(req, cancelToken).ConfigureAwait(false);
        var body = await ReadBody(resp, cancelToken).ConfigureAwait(false);
        var statusCode = (int)resp.StatusCode;
        var isConflict = resp.StatusCode == HttpStatusCode.Conflict || resp.StatusCode == HttpStatusCode.PreconditionFailed;

        if (!isConflict && !resp.IsSuccessStatusCode)
            throw new HttpRequestException($"Sync send failed: {statusCode} {resp.ReasonPhrase} - {body}", null, resp.StatusCode);

        return new SyncSendResult(statusCode, body, isConflict);
    }


    public async Task<SyncPullResult> Pull(SyncEndpoint endpoint, string? cursor, CancellationToken cancelToken)
    {
        var baseUri = endpoint.PullUrl ?? endpoint.Url;
        var uri = AppendCursor(baseUri, endpoint.CursorParameter, cursor);

        using var req = new HttpRequestMessage(HttpMethod.Get, uri);
        await this.RunPullInterceptors(endpoint, cursor, req).ConfigureAwait(false);
        if (endpoint.OnBeforeSend != null)
            await endpoint.OnBeforeSend(req).ConfigureAwait(false);

        using var resp = await this.http.SendAsync(req, cancelToken).ConfigureAwait(false);

        if (resp.StatusCode == HttpStatusCode.NotModified)
            return new SyncPullResult(cursor, Array.Empty<SyncPullItem>(), HasMore: false, NotModified: true);

        resp.EnsureSuccessStatusCode();
        var body = await ReadBody(resp, cancelToken).ConfigureAwait(false);
        return PullParser.Parse(body);
    }


    public async Task<SyncBatchResult> SendBatch(SyncEndpoint endpoint, IReadOnlyList<SyncOperation> ops, CancellationToken cancelToken)
    {
        var uri = endpoint.BatchUrl ?? (endpoint.Url.TrimEnd('/') + "/batch");

        using var ms = new MemoryStream();
        using (var w = new Utf8JsonWriter(ms))
        {
            w.WriteStartObject();
            w.WriteStartArray("operations");
            foreach (var op in ops)
            {
                w.WriteStartObject();
                w.WriteString("id", op.Identifier);
                w.WriteString("entityId", op.EntityIdentifier);
                w.WriteString("verb", op.Verb.ToString());
                if (op.Payload != null)
                {
                    w.WritePropertyName("payload");
                    using var doc = JsonDocument.Parse(op.Payload);
                    doc.RootElement.WriteTo(w);
                }
                w.WriteEndObject();
            }
            w.WriteEndArray();
            w.WriteEndObject();
        }
        var bodyBytes = ms.ToArray();

        using var req = new HttpRequestMessage(HttpMethod.Post, uri)
        {
            Content = new ByteArrayContent(bodyBytes)
        };
        req.Content.Headers.ContentType = new MediaTypeHeaderValue("application/json");

        await this.RunPushInterceptors(endpoint, ops, req).ConfigureAwait(false);
        if (endpoint.OnBeforeSend != null)
            await endpoint.OnBeforeSend(req).ConfigureAwait(false);

        using var resp = await this.http.SendAsync(req, cancelToken).ConfigureAwait(false);
        var body = await ReadBody(resp, cancelToken).ConfigureAwait(false);
        var statusCode = (int)resp.StatusCode;

        if (!resp.IsSuccessStatusCode)
            throw new HttpRequestException($"Sync batch failed: {statusCode} {resp.ReasonPhrase} - {body}", null, resp.StatusCode);

        return BatchResultParser.Parse(statusCode, ops, body);
    }


    public async Task<SyncTombstoneResult> FetchTombstones(SyncEndpoint endpoint, string? cursor, CancellationToken cancelToken)
    {
        if (string.IsNullOrEmpty(endpoint.TombstoneUrl))
            return new SyncTombstoneResult(cursor, Array.Empty<string>());

        var uri = AppendCursor(endpoint.TombstoneUrl, endpoint.TombstoneCursorParameter, cursor);

        using var req = new HttpRequestMessage(HttpMethod.Get, uri);
        await this.RunTombstoneInterceptors(endpoint, cursor, req).ConfigureAwait(false);
        if (endpoint.OnBeforeSend != null)
            await endpoint.OnBeforeSend(req).ConfigureAwait(false);

        using var resp = await this.http.SendAsync(req, cancelToken).ConfigureAwait(false);

        if (resp.StatusCode == HttpStatusCode.NotModified)
            return new SyncTombstoneResult(cursor, Array.Empty<string>(), NotModified: true);

        resp.EnsureSuccessStatusCode();
        var body = await ReadBody(resp, cancelToken).ConfigureAwait(false);
        return TombstoneParser.Parse(body, cursor);
    }


    static string AppendCursor(string baseUri, string? paramName, string? cursor)
    {
        if (cursor == null || string.IsNullOrEmpty(paramName))
            return baseUri;
        var sep = baseUri.Contains('?') ? '&' : '?';
        return baseUri + sep + Uri.EscapeDataString(paramName) + "=" + Uri.EscapeDataString(cursor);
    }


    async Task RunPullInterceptors(SyncEndpoint endpoint, string? cursor, HttpRequestMessage req)
    {
        foreach (var i in this.interceptors)
            await i.BeforePull(endpoint, cursor, req).ConfigureAwait(false);
    }


    async Task RunPushInterceptors(SyncEndpoint endpoint, IReadOnlyList<SyncOperation> ops, HttpRequestMessage req)
    {
        foreach (var i in this.interceptors)
            await i.BeforePush(endpoint, ops, req).ConfigureAwait(false);
    }


    async Task RunTombstoneInterceptors(SyncEndpoint endpoint, string? cursor, HttpRequestMessage req)
    {
        foreach (var i in this.interceptors)
            await i.BeforeTombstoneFetch(endpoint, cursor, req).ConfigureAwait(false);
    }


    static async Task<string?> ReadBody(HttpResponseMessage resp, CancellationToken cancelToken)
    {
        if (resp.Content == null)
            return null;

        try
        {
            return await resp.Content.ReadAsStringAsync(cancelToken).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }
}
