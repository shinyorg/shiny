using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Data.Sync.Infrastructure;
using Shiny.Extensions.Stores.Repositories;
using Shiny.Hosting;

namespace Shiny.Data.Sync;


/// <summary>
/// Apple (iOS / Mac Catalyst) implementation. Outbox sends and inbox pulls both run on a
/// background <see cref="NSUrlSession"/> so they survive app suspension/termination. Outbox
/// uploads are fed from per-operation temp files (background sessions require file-backed
/// uploads); inbox + tombstone pulls run as download tasks and the response body is read
/// from the download location when the task finishes.
/// </summary>
public partial class DataSyncManager(
    ILogger<DataSyncManager> logger,
    IRepository repository,
    IPlatform platform,
    IServiceProvider services,
    SyncEndpointRegistry registry
) :
    NSUrlSessionDataDelegate,
    IDataSyncManager,
    IShinyStartupTask,
    IIosLifecycle.IHandleEventsForBackgroundUrl
{
    const string InboxTaskPrefix = "inbox:";
    const string TombstoneTaskPrefix = "tombstone:";

    Action? completionHandler;
    readonly ConcurrentDictionary<string, MemoryStream> responseBuffers = new();
    bool subscribed;

    public event EventHandler<int>? PendingCountChanged;
    public event EventHandler<SyncOperationResult>? UpdateReceived;
    public event EventHandler<SyncPullCompletion>? PullCompleted;
    public event EventHandler<SyncEvent>? Activity;


    public async void Start()
    {
        if (!this.subscribed)
        {
            repository.ActionOccurred += this.OnRepoAction;
            this.subscribed = true;
        }

        try
        {
            var tasks = await this.Session.GetAllTasksAsync();
            foreach (var task in tasks)
                task.Resume();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error auto-starting Data Sync");
        }
    }


    protected override void Dispose(bool disposing)
    {
        if (disposing && this.subscribed)
        {
            repository.ActionOccurred -= this.OnRepoAction;
            this.subscribed = false;
        }
        base.Dispose(disposing);
    }


    void OnRepoAction(object? sender, (RepositoryAction Action, Type EntityType, IRepositoryEntity? Entity) x)
    {
        if (x.EntityType != typeof(SyncOperation) || x.Action == RepositoryAction.Update)
            return;
        this.PendingCountChanged?.Invoke(this, repository.GetAll<SyncOperation>().Count);
    }


    string SessionIdentifier => $"{NSBundle.MainBundle.BundleIdentifier}.Shiny.DataSync";

    NSUrlSession? nsUrlSession;
    public NSUrlSession Session
    {
        get
        {
            if (this.nsUrlSession == null)
            {
                var cfg = NSUrlSessionConfiguration.CreateBackgroundSessionConfiguration(this.SessionIdentifier);
                cfg.HttpMaximumConnectionsPerHost = 4;
                cfg.SessionSendsLaunchEvents = true;
                this.nsUrlSession = NSUrlSession.FromConfiguration(cfg, this, new NSOperationQueue());
            }
            return this.nsUrlSession!;
        }
    }


    public Task<IList<SyncOperation>> GetPending()
    {
        var list = repository.GetAll<SyncOperation>().ToList();
        return Task.FromResult<IList<SyncOperation>>(list);
    }


    public async Task<SyncOperation> Queue<T>(SyncVerb verb, T entity) where T : ISyncEntity
    {
        var op = HttpClientDataSyncManager.BuildOperation(registry, verb, entity);
        var endpoint = registry.Get(op.EndpointKey)!;

        try
        {
            repository.Insert(op);
            this.RaiseActivity(new SyncEvent(SyncEventType.OutboxQueued, op.EndpointKey, Operation: op));
            var task = await this.CreateUploadTask(endpoint, op).ConfigureAwait(false);
            task.TaskDescription = op.Identifier;
            task.Resume();
            return op;
        }
        catch
        {
            repository.Remove<SyncOperation>(op.Identifier);
            throw;
        }
    }


    public Task PullNow<T>(CancellationToken cancelToken = default) where T : ISyncEntity
    {
        var endpoint = registry.Get<T>()
            ?? throw new InvalidOperationException($"No sync endpoint registered for type {typeof(T).FullName}");
        if (endpoint.Direction == SyncDirection.PushOnly)
            throw new InvalidOperationException($"Endpoint '{endpoint.Key}' is configured PushOnly - PullNow is not allowed.");
        return this.PullEndpoint(endpoint, force: true, cancelToken);
    }


    public Task PullAll(CancellationToken cancelToken = default)
    {
        var pullable = registry.All.Where(e => e.Direction != SyncDirection.PushOnly);
        foreach (var endpoint in pullable)
        {
            if (cancelToken.IsCancellationRequested)
                break;
            _ = this.PullEndpoint(endpoint, force: false, cancelToken);
        }
        return Task.CompletedTask;
    }


    public async Task Cancel(string operationId)
    {
        var tasks = await this.Session.GetAllTasksAsync();
        var match = tasks.FirstOrDefault(x =>
            IsOutboxTask(x)
            && string.Equals(x.TaskDescription, operationId, StringComparison.InvariantCultureIgnoreCase));
        match?.Cancel();
    }


    public async Task CancelAll()
    {
        var tasks = await this.Session.GetAllTasksAsync();
        foreach (var t in tasks.Where(IsOutboxTask))
            t.Cancel();
    }


    public async Task CancelAll<T>() where T : ISyncEntity
    {
        var key = typeof(T).FullName!;
        var tasks = await this.Session.GetAllTasksAsync();
        foreach (var t in tasks.Where(IsOutboxTask))
        {
            var op = repository.Get<SyncOperation>(t.TaskDescription!);
            if (op != null && op.EndpointKey == key)
                t.Cancel();
        }
    }


    static bool IsOutboxTask(NSUrlSessionTask t)
        => t.TaskDescription != null
            && !t.TaskDescription.StartsWith(InboxTaskPrefix, StringComparison.Ordinal)
            && !t.TaskDescription.StartsWith(TombstoneTaskPrefix, StringComparison.Ordinal);


    public new bool Handle(string sessionIdentifier, Action incomingCompletionHandler)
    {
        if (!this.SessionIdentifier.Equals(sessionIdentifier, StringComparison.InvariantCulture))
            return false;

        _ = this.Session;
        this.completionHandler = incomingCompletionHandler;
        return true;
    }


    async Task<NSUrlSessionUploadTask> CreateUploadTask(SyncEndpoint endpoint, SyncOperation op)
    {
        var (method, uri) = op.Verb switch
        {
            SyncVerb.Create => ("POST", endpoint.Url),
            SyncVerb.Update => ("PUT", $"{endpoint.Url.TrimEnd('/')}/{Uri.EscapeDataString(op.EntityIdentifier)}"),
            SyncVerb.Delete => ("DELETE", $"{endpoint.Url.TrimEnd('/')}/{Uri.EscapeDataString(op.EntityIdentifier)}"),
            _ => throw new InvalidOperationException("Unknown sync verb: " + op.Verb)
        };

        var url = NSUrl.FromString(uri)!;
        var native = new NSMutableUrlRequest(url)
        {
            HttpMethod = method,
            AllowsExpensiveNetworkAccess = endpoint.UseMeteredConnection
        };
        native["Content-Type"] = "application/json";

        await this.ApplyPushHooks(endpoint, native, method, uri, op).ConfigureAwait(false);

        var payload = op.Payload ?? string.Empty;
        var tempPath = AppleSyncTempFiles.GetUploadTempFilePath(platform, op.Identifier);
        File.WriteAllText(tempPath, payload);

        var fileUrl = NSUrl.CreateFileUrl(tempPath, null);
        return this.Session.CreateUploadTask(native, fileUrl);
    }


    async Task ApplyPushHooks(SyncEndpoint endpoint, NSMutableUrlRequest native, string method, string uri, SyncOperation op)
    {
        using var stub = new HttpRequestMessage(new HttpMethod(method), uri);

        foreach (var i in this.GetInterceptors())
            await i.BeforePush(endpoint, new[] { op }, stub).ConfigureAwait(false);

        if (endpoint.OnBeforeSend != null)
            await endpoint.OnBeforeSend(stub).ConfigureAwait(false);

        CopyHeaders(stub, native);
    }


    /// <summary>
    /// Translates a stub <see cref="HttpRequestMessage"/>'s headers onto the native
    /// <see cref="NSMutableUrlRequest"/>. The content body itself is not transferred (uploads
    /// stream from disk, downloads have no body) — signers that hash the body won't work on Apple.
    /// </summary>
    internal static async Task ApplyPullHook(
        IEnumerable<ISyncInterceptor> interceptors,
        SyncEndpoint endpoint,
        string? cursor,
        bool isTombstone,
        NSMutableUrlRequest native,
        string method,
        string uri
    )
    {
        using var stub = new HttpRequestMessage(new HttpMethod(method), uri);

        foreach (var i in interceptors)
        {
            if (isTombstone)
                await i.BeforeTombstoneFetch(endpoint, cursor, stub).ConfigureAwait(false);
            else
                await i.BeforePull(endpoint, cursor, stub).ConfigureAwait(false);
        }

        if (endpoint.OnBeforeSend != null)
            await endpoint.OnBeforeSend(stub).ConfigureAwait(false);

        CopyHeaders(stub, native);
    }


    static void CopyHeaders(HttpRequestMessage stub, NSMutableUrlRequest native)
    {
        foreach (var h in stub.Headers)
            native[h.Key] = string.Join(",", h.Value);

        if (stub.Content?.Headers != null)
        {
            foreach (var h in stub.Content.Headers)
                native[h.Key] = string.Join(",", h.Value);
        }
    }


    void TryDeleteTempFile(string operationId)
    {
        var path = AppleSyncTempFiles.GetUploadTempFilePath(platform, operationId);
        if (File.Exists(path))
        {
            try { File.Delete(path); }
            catch (Exception ex) { logger.LogWarning(ex, "Failed to delete sync tmp file for {id}", operationId); }
        }
    }


    void TryCompleteSession()
    {
        var ops = repository.GetAll<SyncOperation>();
        if (ops.Count == 0)
        {
            this.completionHandler?.Invoke();
            this.completionHandler = null;
        }
    }


    void RaisePullCompleted(SyncPullCompletion completion)
        => this.PullCompleted?.Invoke(this, completion);


    void RaiseActivity(SyncEvent evt)
        => this.Activity?.Invoke(this, evt);


    IEnumerable<IDataSyncDelegate> GetDelegates() => services.GetServices<IDataSyncDelegate>();


    IEnumerable<ISyncInterceptor> GetInterceptors() => services.GetServices<ISyncInterceptor>();
}
