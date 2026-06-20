using System;
using System.Collections.Concurrent;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Extensions.Logging;
using Shiny.Data.Sync.Infrastructure;

namespace Shiny.Data.Sync;


public partial class DataSyncManager
{
    sealed class InboxRequest
    {
        public required SyncEndpoint Endpoint { get; init; }
        public required bool IsTombstone { get; init; }
        public string? StartingCursor { get; init; }
        public CancellationToken CancelToken { get; init; }
        public CancellationTokenRegistration CancelRegistration { get; set; }
    }

    readonly ConcurrentDictionary<string, InboxRequest> inboxRequests = new();


    /// <summary>
    /// Starts an NSURLSession download task for the endpoint, attaching the persisted cursor
    /// (when present) under <see cref="SyncEndpoint.CursorParameter"/>. The session is
    /// configured so the task survives app suspension; results are picked up in
    /// <see cref="DidFinishDownloading"/> on completion.
    /// </summary>
    async Task PullEndpoint(SyncEndpoint endpoint, bool force, CancellationToken cancelToken)
    {
        if (endpoint.Direction == SyncDirection.PushOnly)
            return;

        if (!force && this.IsThrottled(endpoint))
        {
            logger.LogDebug("Skipping pull for {key} - throttled by MinPullInterval", endpoint.Key);
            return;
        }

        // Coalesce overlapping pulls - skip if a download task for this endpoint is already pending.
        var taskDescription = InboxTaskPrefix + endpoint.Key;
        if (this.inboxRequests.ContainsKey(taskDescription))
        {
            logger.LogDebug("Skipping pull for {key}: a pull is already in progress", endpoint.Key);
            return;
        }

        var cursor = repository.Get<SyncCursor>(endpoint.Key)?.Cursor;
        var pullUrl = endpoint.PullUrl ?? endpoint.Url;
        var uri = AppendCursor(pullUrl, endpoint.CursorParameter, cursor);

        logger.LogInformation("Starting inbox pull for {key} (cursor: {cursor})", endpoint.Key, cursor ?? "<none>");
        this.RaiseActivity(new SyncEvent(SyncEventType.InboxPullStarted, endpoint.Key, Cursor: cursor));

        await this.StartDownload(endpoint, uri, taskDescription, isTombstone: false, cursor, cancelToken).ConfigureAwait(false);
    }


    async Task StartTombstoneFetch(SyncEndpoint endpoint, CancellationToken cancelToken)
    {
        if (string.IsNullOrEmpty(endpoint.TombstoneUrl))
            return;

        var cursor = repository.Get<SyncTombstoneCursor>(endpoint.Key)?.Cursor;
        var uri = AppendCursor(endpoint.TombstoneUrl, endpoint.TombstoneCursorParameter, cursor);

        await this.StartDownload(endpoint, uri, TombstoneTaskPrefix + endpoint.Key, isTombstone: true, cursor, cancelToken).ConfigureAwait(false);
    }


    async Task StartDownload(SyncEndpoint endpoint, string uri, string taskDescription, bool isTombstone, string? startingCursor, CancellationToken cancelToken)
    {
        try
        {
            var url = NSUrl.FromString(uri)!;
            var native = new NSMutableUrlRequest(url)
            {
                HttpMethod = "GET",
                AllowsExpensiveNetworkAccess = endpoint.UseMeteredConnection
            };
            native["Accept"] = "application/json";

            await ApplyPullHook(this.GetInterceptors(), endpoint, startingCursor, isTombstone, native, "GET", uri).ConfigureAwait(false);

            var task = this.Session.CreateDownloadTask(native);
            task.TaskDescription = taskDescription;

            var ctx = new InboxRequest
            {
                Endpoint = endpoint,
                IsTombstone = isTombstone,
                StartingCursor = startingCursor,
                CancelToken = cancelToken
            };
            this.inboxRequests[taskDescription] = ctx;
            if (cancelToken.CanBeCanceled)
                ctx.CancelRegistration = cancelToken.Register(() => task.Cancel());

            task.Resume();
        }
        catch (Exception ex)
        {
            var label = isTombstone ? "tombstone" : "inbox";
            logger.LogError(ex, "Failed to start {label} download for {key}", label, endpoint.Key);
            if (!isTombstone)
            {
                var existing = repository.Get<SyncCursor>(endpoint.Key)?.Cursor;
                this.RaisePullCompleted(new SyncPullCompletion(endpoint.Key, 0, existing, ex));
                this.RaiseActivity(new SyncEvent(SyncEventType.InboxPullFailed, endpoint.Key, Cursor: existing, Error: ex));
            }
        }
    }


    /// <summary>
    /// NSURLSessionDownloadDelegate hook. Triggered on the background session when a sync
    /// inbox or tombstone download finishes — including after app relaunch if the download
    /// spanned a suspend.
    /// </summary>
    [Export("URLSession:downloadTask:didFinishDownloadingToURL:")]
    public void DidFinishDownloading(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, NSUrl location)
    {
        var desc = downloadTask.TaskDescription;
        if (desc == null)
            return;

        var isTombstone = desc.StartsWith(TombstoneTaskPrefix, StringComparison.Ordinal);
        var isInbox = desc.StartsWith(InboxTaskPrefix, StringComparison.Ordinal);
        if (!isTombstone && !isInbox)
            return;

        var endpointKey = desc.Substring((isTombstone ? TombstoneTaskPrefix : InboxTaskPrefix).Length);
        var endpoint = registry.Get(endpointKey);
        if (endpoint == null)
        {
            logger.LogWarning("Download finished but endpoint {key} is no longer registered", endpointKey);
            this.CleanupInboxRequest(desc);
            return;
        }

        var statusCode = (int)((downloadTask.Response as NSHttpUrlResponse)?.StatusCode ?? 0);

        if (statusCode == 304)
        {
            logger.LogDebug("{label} pull for {key} returned 304", isTombstone ? "Tombstone" : "Inbox", endpointKey);
            if (!isTombstone)
            {
                var existing = repository.Get<SyncCursor>(endpoint.Key)?.Cursor;
                repository.Set(new SyncCursor(endpoint.Key, existing, DateTimeOffset.UtcNow));
                this.RaisePullCompleted(new SyncPullCompletion(endpoint.Key, 0, existing, null));
                this.RaiseActivity(new SyncEvent(SyncEventType.InboxPullCompleted, endpoint.Key, ItemCount: 0, Cursor: existing));
            }
            this.CleanupInboxRequest(desc);
            this.TryCompleteSession();
            return;
        }

        if (statusCode is < 200 or > 299)
        {
            var err = new InvalidOperationException($"{(isTombstone ? "Tombstone" : "Inbox")} pull for {endpointKey} returned status {statusCode}");
            logger.LogWarning(err, "non-success status");
            if (!isTombstone)
            {
                var existing = repository.Get<SyncCursor>(endpoint.Key)?.Cursor;
                this.RaisePullCompleted(new SyncPullCompletion(endpoint.Key, 0, existing, err));
                this.RaiseActivity(new SyncEvent(SyncEventType.InboxPullFailed, endpoint.Key, StatusCode: statusCode, Cursor: existing, Error: err));
            }
            this.CleanupInboxRequest(desc);
            this.TryCompleteSession();
            return;
        }

        string? body = null;
        try
        {
            var path = location.Path;
            if (!string.IsNullOrEmpty(path) && File.Exists(path))
                body = File.ReadAllText(path);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to read download body for {key}", endpointKey);
            this.CleanupInboxRequest(desc);
            this.TryCompleteSession();
            return;
        }

        if (isTombstone)
        {
            var tombResult = TombstoneParser.Parse(body, repository.Get<SyncTombstoneCursor>(endpoint.Key)?.Cursor);
            _ = this.DispatchTombstones(endpoint, desc, tombResult);
        }
        else
        {
            SyncPullResult result;
            try { result = PullParser.Parse(body); }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to parse inbox payload for {key}", endpointKey);
                this.RaisePullCompleted(new SyncPullCompletion(endpoint.Key, 0, repository.Get<SyncCursor>(endpoint.Key)?.Cursor, ex));
                this.RaiseActivity(new SyncEvent(SyncEventType.InboxPullFailed, endpoint.Key, Error: ex));
                this.CleanupInboxRequest(desc);
                this.TryCompleteSession();
                return;
            }
            _ = this.DispatchInbox(endpoint, desc, result);
        }
    }


    async Task DispatchInbox(SyncEndpoint endpoint, string taskDescription, SyncPullResult result)
    {
        var dispatched = 0;
        foreach (var item in result.Items)
        {
            object? entity = null;
            try { entity = SyncInboxProcessor.DeserializeEntity(endpoint, item.Payload); }
            catch (Exception ex)
            {
                logger.LogError(ex, "Failed to deserialize inbox payload for {key}, entity {id}", endpoint.Key, item.EntityIdentifier);
            }

            var verb = item.Verb;
            if (entity != null && verb != SyncVerb.Delete)
            {
                if (endpoint.SoftDeletePredicate?.Invoke(entity) == true || endpoint.ExpiryPredicate?.Invoke(entity) == true)
                    verb = SyncVerb.Delete;
            }

            var received = new SyncReceivedItem(endpoint.Key, endpoint.EntityType, entity, item.Payload, verb);
            foreach (var d in this.GetDelegates())
            {
                try { await d.OnReceived(received).ConfigureAwait(false); }
                catch (Exception ex) { logger.LogError(ex, "Delegate OnReceived threw for endpoint {key}, entity {id}", endpoint.Key, item.EntityIdentifier); }
            }
            this.RaiseActivity(new SyncEvent(SyncEventType.InboxItemReceived, endpoint.Key, Item: received));
            dispatched++;
        }

        var existing = repository.Get<SyncCursor>(endpoint.Key)?.Cursor;
        var nextCursor = result.NextCursor ?? existing;
        repository.Set(new SyncCursor(endpoint.Key, nextCursor, DateTimeOffset.UtcNow));

        this.RaisePullCompleted(new SyncPullCompletion(endpoint.Key, dispatched, nextCursor, null));
        this.RaiseActivity(new SyncEvent(SyncEventType.InboxPullCompleted, endpoint.Key, ItemCount: dispatched, Cursor: nextCursor));

        var followupToken = this.inboxRequests.TryGetValue(taskDescription, out var ctx) ? ctx.CancelToken : CancellationToken.None;
        this.CleanupInboxRequest(taskDescription);

        if (result.HasMore && !followupToken.IsCancellationRequested)
            await this.PullEndpoint(endpoint, force: true, followupToken).ConfigureAwait(false);
        else if (!string.IsNullOrEmpty(endpoint.TombstoneUrl) && !followupToken.IsCancellationRequested)
            await this.StartTombstoneFetch(endpoint, followupToken).ConfigureAwait(false);

        this.TryCompleteSession();
    }


    async Task DispatchTombstones(SyncEndpoint endpoint, string taskDescription, SyncTombstoneResult result)
    {
        foreach (var id in result.DeletedIdentifiers)
        {
            var received = new SyncReceivedItem(endpoint.Key, endpoint.EntityType, Entity: null, RawPayload: "null", SyncVerb.Delete);
            foreach (var d in this.GetDelegates())
            {
                try { await d.OnReceived(received).ConfigureAwait(false); }
                catch (Exception ex) { logger.LogError(ex, "Delegate OnReceived threw for tombstone {key}/{id}", endpoint.Key, id); }
            }
            this.RaiseActivity(new SyncEvent(SyncEventType.InboxItemReceived, endpoint.Key, Item: received));
        }

        var existing = repository.Get<SyncTombstoneCursor>(endpoint.Key)?.Cursor;
        var nextCursor = result.NextCursor ?? existing;
        repository.Set(new SyncTombstoneCursor(endpoint.Key, nextCursor, DateTimeOffset.UtcNow));

        if (result.DeletedIdentifiers.Count > 0)
            this.RaiseActivity(new SyncEvent(SyncEventType.TombstonesApplied, endpoint.Key, ItemCount: result.DeletedIdentifiers.Count, Cursor: nextCursor));

        this.CleanupInboxRequest(taskDescription);
        this.TryCompleteSession();
    }


    bool IsThrottled(SyncEndpoint endpoint)
    {
        // Null = manual-only (skip automatic pulls); TimeSpan.Zero = always pull.
        if (!endpoint.MinPullInterval.HasValue)
            return true;
        var last = repository.Get<SyncCursor>(endpoint.Key)?.LastPulledAt;
        if (!last.HasValue)
            return false;
        return DateTimeOffset.UtcNow - last.Value < endpoint.MinPullInterval.Value;
    }


    static string AppendCursor(string baseUri, string? paramName, string? cursor)
    {
        if (cursor == null || string.IsNullOrEmpty(paramName))
            return baseUri;
        var sep = baseUri.Contains('?') ? '&' : '?';
        return baseUri + sep + Uri.EscapeDataString(paramName) + "=" + Uri.EscapeDataString(cursor);
    }


    void CleanupInboxRequest(string taskDescription)
    {
        if (this.inboxRequests.TryRemove(taskDescription, out var ctx))
            ctx.CancelRegistration.Dispose();
    }
}
