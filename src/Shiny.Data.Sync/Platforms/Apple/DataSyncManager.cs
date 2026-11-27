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
    bool backgroundEventsFinished;
    int pendingDelegateCalls;
    readonly ConcurrentDictionary<string, MemoryStream> responseBuffers = new();

    // ops this process has started a native task for - Start() must not treat them as stranded while their
    // task is still being created (Queue awaits interceptors before the task exists)
    readonly ConcurrentDictionary<string, byte> startedThisProcess = new(StringComparer.InvariantCultureIgnoreCase);
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

            // An op waiting out a retry delay (or re-queued after a KeepLocal conflict) has no native task -
            // the in-process delay died with the app. Send those again now.
            var running = tasks
                .Where(IsOutboxTask)
                .Select(x => x.TaskDescription!)
                .ToHashSet(StringComparer.InvariantCultureIgnoreCase);

            // Ops left Pending / InProgress with no retry marker and no native task (stranded by older versions,
            // or any other path that loses the task) can't be in flight - mark them ready so this pass sends them.
            this.MarkUntrackedReady(running);

            var stranded = repository
                .GetAll<SyncOperation>()
                .Where(x => x.NextAttemptAt != null && !running.Contains(x.Identifier))
                .OrderBy(x => x.CreatedAt)
                .ToList();

            foreach (var op in stranded)
                await this.ResendStranded(op).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error auto-starting Data Sync");
        }
    }


    void MarkUntrackedReady(HashSet<string> running)
    {
        var tracked = new HashSet<string>(running, StringComparer.InvariantCultureIgnoreCase);
        tracked.UnionWith(this.startedThisProcess.Keys);

        var now = DateTimeOffset.UtcNow;
        var untracked = UntrackedOperations.Select(repository.GetAll<SyncOperation>(), tracked);
        foreach (var op in untracked)
        {
            logger.LogInformation("Sync op {id} has no native task - re-sending", op.Identifier);
            repository.Set(op with { NextAttemptAt = now });
        }
    }


    async Task ResendStranded(SyncOperation op)
    {
        var endpoint = registry.Get(op.EndpointKey);
        if (endpoint == null)
        {
            logger.LogWarning("No endpoint registered for {key} - leaving op {id} queued", op.EndpointKey, op.Identifier);
            return;
        }

        try
        {
            await this.SendOperation(endpoint, op).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.OnError(op, 0, ex);
        }
    }


    /// <summary>
    /// Starts the native upload task for an op. The attempt count goes up and the retry marker
    /// (<see cref="SyncOperation.NextAttemptAt"/>) is cleared before the task runs, so a fast completion
    /// can't be overwritten and <see cref="Start"/> can tell an op with a live task from a stranded one.
    /// </summary>
    async Task SendOperation(SyncEndpoint endpoint, SyncOperation op)
    {
        this.startedThisProcess[op.Identifier] = 0;
        var task = await this.CreateUploadTask(endpoint, op).ConfigureAwait(false);
        task.TaskDescription = op.Identifier;
        repository.Set(op with { Attempts = op.Attempts + 1, NextAttemptAt = null });
        task.Resume();
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

        if (x.Action == RepositoryAction.Remove && x.Entity != null)
            this.startedThisProcess.TryRemove(x.Entity.Identifier, out _);
        else if (x.Action == RepositoryAction.Clear)
            this.startedThisProcess.Clear();

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
            await this.SendOperation(endpoint, op).ConfigureAwait(false);
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

        if (match != null)
        {
            match.Cancel();
        }
        else
        {
            // waiting for a retry - there is no native task to cancel
            var op = repository.Get<SyncOperation>(operationId);
            if (op != null)
                this.OnCancel(op);
        }
    }


    public async Task CancelAll()
    {
        var tasks = await this.Session.GetAllTasksAsync();
        foreach (var t in tasks.Where(IsOutboxTask))
            t.Cancel();

        this.CancelOpsWithoutTask(tasks, _ => true);
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

        this.CancelOpsWithoutTask(tasks, op => op.EndpointKey == key);
    }


    void CancelOpsWithoutTask(NSUrlSessionTask[] tasks, Func<SyncOperation, bool> predicate)
    {
        var running = tasks
            .Where(IsOutboxTask)
            .Select(x => x.TaskDescription!)
            .ToHashSet(StringComparer.InvariantCultureIgnoreCase);

        var idle = repository
            .GetAll<SyncOperation>()
            .Where(x => predicate(x) && !running.Contains(x.Identifier))
            .ToList();

        foreach (var op in idle)
            this.OnCancel(op);
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
        this.backgroundEventsFinished = false;
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


    /// <summary>
    /// Marks the start of a delegate dispatch (<see cref="IDataSyncDelegate"/> calls and the work around them). The
    /// background-session completion handler is held until every dispatch has ended.
    /// </summary>
    void BeginDelegateDispatch() => Interlocked.Increment(ref this.pendingDelegateCalls);


    void EndDelegateDispatch()
    {
        Interlocked.Decrement(ref this.pendingDelegateCalls);
        this.TryCompleteSession();
    }


    /// <summary>
    /// Hands iOS its background-session completion handler back once - only after
    /// <see cref="DidFinishEventsForBackgroundSession"/> and with no delegate dispatch still running, so the app
    /// isn't suspended in the middle of <see cref="IDataSyncDelegate.OnSent"/> / <see cref="IDataSyncDelegate.OnReceived"/>.
    /// </summary>
    void TryCompleteSession()
    {
        if (Volatile.Read(ref this.pendingDelegateCalls) > 0)
            return;

        if (!this.backgroundEventsFinished)
            return;

        var handler = Interlocked.Exchange(ref this.completionHandler, null);
        if (handler != null)
        {
            this.backgroundEventsFinished = false;
            handler.Invoke();
        }
    }


    void RaisePullCompleted(SyncPullCompletion completion)
        => this.PullCompleted?.Invoke(this, completion);


    void RaiseActivity(SyncEvent evt)
        => this.Activity?.Invoke(this, evt);


    IEnumerable<IDataSyncDelegate> GetDelegates() => services.GetServices<IDataSyncDelegate>();


    IEnumerable<ISyncInterceptor> GetInterceptors() => services.GetServices<ISyncInterceptor>();
}
