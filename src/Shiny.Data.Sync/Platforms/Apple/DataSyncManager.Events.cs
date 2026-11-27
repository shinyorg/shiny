using System;
using System.IO;
using System.Text;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Extensions.Logging;

namespace Shiny.Data.Sync;


public partial class DataSyncManager
{
    public override void DidReceiveData(NSUrlSession session, NSUrlSessionDataTask dataTask, NSData data)
    {
        var id = dataTask.TaskDescription;
        if (id == null || id.StartsWith(InboxTaskPrefix, StringComparison.Ordinal))
            return;

        var buffer = this.responseBuffers.GetOrAdd(id, _ => new MemoryStream());
        var bytes = data.ToArray();
        buffer.Write(bytes, 0, bytes.Length);
    }


    public override void DidSendBodyData(NSUrlSession session, NSUrlSessionTask task, long bytesSent, long totalBytesSent, long totalBytesExpectedToSend)
    {
        var id = task.TaskDescription;
        if (id == null || id.StartsWith(InboxTaskPrefix, StringComparison.Ordinal))
            return;

        var op = repository.Get<SyncOperation>(id);
        if (op == null || op.State == SyncOperationState.InProgress)
            return;

        var updated = op with { State = SyncOperationState.InProgress };
        try
        {
            repository.Update(updated);
            this.UpdateReceived?.Invoke(this, new SyncOperationResult(updated, SyncOperationState.InProgress, null, null));
            this.RaiseActivity(new SyncEvent(SyncEventType.OutboxStarted, updated.EndpointKey, Operation: updated));
        }
        catch (Shiny.Extensions.Stores.Repositories.RepositoryException)
        {
            // ignore
        }
    }


    public override void DidCompleteWithError(NSUrlSession session, NSUrlSessionTask task, NSError? error)
    {
        var id = task.TaskDescription;
        if (id == null)
            return;

        // Inbox tasks: success is handled in DidFinishDownloading; this fires for errors only.
        if (id.StartsWith(InboxTaskPrefix, StringComparison.Ordinal))
        {
            if (error != null)
            {
                var endpointKey = id.Substring(InboxTaskPrefix.Length);
                var ex = new InvalidOperationException(error.LocalizedDescription);
                logger.LogError(ex, "Inbox pull failed for {key}", endpointKey);
                this.RaisePullCompleted(new SyncPullCompletion(
                    endpointKey,
                    0,
                    repository.Get<SyncCursor>(endpointKey)?.Cursor,
                    ex
                ));
                this.CleanupInboxRequest(id);
            }
            this.TryCompleteSession();
            return;
        }

        var op = repository.Get<SyncOperation>(id);
        if (op == null)
        {
            logger.NoOperationFound(id);
            this.responseBuffers.TryRemove(id, out _);
            this.TryDeleteTempFile(id);
            return;
        }

        var statusCode = (int)((task.Response as NSHttpUrlResponse)?.StatusCode ?? 0);
        string? body = null;
        if (this.responseBuffers.TryRemove(id, out var buf))
        {
            try { body = Encoding.UTF8.GetString(buf.ToArray()); }
            catch { body = null; }
            buf.Dispose();
        }

        if (task.State == NSUrlSessionTaskState.Canceling || (error?.Code ?? 0) == -999)
        {
            this.OnCancel(op);
            return;
        }

        if (error != null)
        {
            var msg = $"Sync transport error - {error.LocalizedDescription} - {error.LocalizedFailureReason}";
            this.OnError(op, statusCode, new InvalidOperationException(msg));
            return;
        }

        if (statusCode == 409 || statusCode == 412)
        {
            this.OnConflict(op, statusCode, body ?? string.Empty);
            return;
        }

        var endpoint = registry.Get(op.EndpointKey);
        if (endpoint != null && endpoint.IsAlreadyDeleted(op.Verb, statusCode))
        {
            // the entity is gone either way - a retried DELETE whose first try succeeded, or a Create that never landed
            logger.StandardInfo(op.Identifier, $"DELETE returned {statusCode} - entity already gone, completing");
            this.OnFinish(op, statusCode, body);
            return;
        }

        if (statusCode < 200 || statusCode > 299)
        {
            this.OnError(op, statusCode, new InvalidOperationException("Sync transport error - non-success status: " + statusCode));
            return;
        }

        this.OnFinish(op, statusCode, body);
    }


    public override void DidBecomeInvalid(NSUrlSession session, NSError? error)
    {
        logger.LogDebug("Data sync NSUrlSession DidBecomeInvalid");
        this.nsUrlSession = null;
        if (error != null)
            logger.LogError(new InvalidOperationException(error.LocalizedDescription), "DidBecomeInvalid reported an error");
    }


    public override void DidFinishEventsForBackgroundSession(NSUrlSession session)
    {
        logger.LogInformation("Data sync DidFinishEventsForBackgroundSession");
        this.backgroundEventsFinished = true;
        this.TryCompleteSession();
    }


    async void OnFinish(SyncOperation op, int statusCode, string? responseBody)
    {
        this.BeginDelegateDispatch();
        try
        {
            logger.OperationUpdate(op.Identifier, SyncOperationState.Completed);
            this.TryDeleteTempFile(op.Identifier);
            repository.Remove(op);

            await services.RunDelegates<IDataSyncDelegate>(d => d.OnSent(op, responseBody), logger);

            this.UpdateReceived?.Invoke(this, new SyncOperationResult(op, SyncOperationState.Completed, statusCode, null));
            this.RaiseActivity(new SyncEvent(SyncEventType.OutboxSent, op.EndpointKey, Operation: op, StatusCode: statusCode));
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error completing sync op {id}", op.Identifier);
        }
        finally
        {
            this.EndDelegateDispatch();
        }
    }


    async void OnError(SyncOperation op, int statusCode, Exception ex)
    {
        var endpoint = registry.Get(op.EndpointKey);
        var isTransient = statusCode == 0 || statusCode >= 500 || statusCode == 408 || statusCode == 429;

        if (endpoint != null && isTransient && op.Attempts < endpoint.MaxAttempts)
        {
            await this.RetryOperation(endpoint, op, statusCode, ex).ConfigureAwait(false);
            return;
        }

        this.BeginDelegateDispatch();
        try
        {
            logger.LogError(ex, "Sync op {id} failed permanently (status {code}, attempts {attempts})", op.Identifier, statusCode, op.Attempts);
            this.TryDeleteTempFile(op.Identifier);
            repository.Remove(op);

            await services.RunDelegates<IDataSyncDelegate>(d => d.OnError(op, statusCode, ex), logger);

            this.UpdateReceived?.Invoke(this, new SyncOperationResult(op, SyncOperationState.Error, statusCode, ex));
            this.RaiseActivity(new SyncEvent(SyncEventType.OutboxFailed, op.EndpointKey, Operation: op, StatusCode: statusCode, Error: ex));
        }
        catch (Exception fex)
        {
            logger.LogError(fex, "Error failing sync op {id}", op.Identifier);
        }
        finally
        {
            this.EndDelegateDispatch();
        }
    }


    async Task RetryOperation(SyncEndpoint endpoint, SyncOperation op, int statusCode, Exception ex)
    {
        var delayMs = (long)endpoint.RetryBaseDelay.TotalMilliseconds * (long)Math.Pow(2, Math.Max(0, op.Attempts - 1));
        var delay = TimeSpan.FromMilliseconds(Math.Min(delayMs, TimeSpan.FromSeconds(60).TotalMilliseconds));
        logger.StandardWarn(op.Identifier,
            $"Transient error (status {statusCode}, attempts {op.Attempts}/{endpoint.MaxAttempts}) - retry in {delay.TotalSeconds:N1}s: {ex.Message}");

        this.TryDeleteTempFile(op.Identifier);

        // NextAttemptAt marks the op as having no native task, so Start() re-sends it if the app dies
        // before the in-process delay below runs out
        var retry = op with { State = SyncOperationState.Pending, LastError = ex.Message, NextAttemptAt = DateTimeOffset.UtcNow + delay };
        repository.Set(retry);
        this.UpdateReceived?.Invoke(this, new SyncOperationResult(retry, SyncOperationState.Pending, statusCode, ex));
        this.RaiseActivity(new SyncEvent(SyncEventType.OutboxRetryScheduled, retry.EndpointKey, Operation: retry, StatusCode: statusCode, Error: ex));

        await Task.Delay(delay).ConfigureAwait(false);

        // re-read - cancelled, or already re-sent by Start(), while we waited
        var current = repository.Get<SyncOperation>(retry.Identifier);
        if (current?.NextAttemptAt == null)
            return;

        try
        {
            await this.SendOperation(endpoint, current).ConfigureAwait(false);
        }
        catch (Exception inner)
        {
            this.OnError(current, 0, inner);
        }
    }


    async void OnConflict(SyncOperation op, int statusCode, string remotePayload)
    {
        var endpoint = registry.Get(op.EndpointKey);
        if (endpoint == null)
        {
            this.OnError(op, statusCode, new InvalidOperationException($"No endpoint registered for {op.EndpointKey}"));
            return;
        }

        this.BeginDelegateDispatch();
        try
        {
            this.RaiseActivity(new SyncEvent(SyncEventType.OutboxConflict, op.EndpointKey, Operation: op, StatusCode: statusCode));

            var resolution = endpoint.DefaultConflictPolicy switch
            {
                ConflictPolicy.ServerWins => ConflictResolution.AcceptRemote,
                ConflictPolicy.ClientWins => ConflictResolution.KeepLocal,
                _ => await this.AskDelegateForConflict(op, remotePayload)
            };

            switch (resolution.Action)
            {
                case ConflictAction.AcceptRemote:
                    this.TryDeleteTempFile(op.Identifier);
                    repository.Remove(op);
                    await this.DispatchConflictRemote(endpoint, op.EntityIdentifier, remotePayload).ConfigureAwait(false);
                    this.UpdateReceived?.Invoke(this, new SyncOperationResult(op, SyncOperationState.Completed, statusCode, null));
                    break;

                case ConflictAction.KeepLocal:
                case ConflictAction.UseMerged:
                    this.TryDeleteTempFile(op.Identifier);
                    var retry = resolution.Action == ConflictAction.UseMerged
                        ? op with { Payload = resolution.MergedPayload, State = SyncOperationState.Pending }
                        : op with { State = SyncOperationState.Pending };

                    if (op.Attempts >= endpoint.MaxAttempts)
                    {
                        // the server keeps rejecting it - fail instead of re-sending it forever
                        this.OnError(retry, statusCode, new InvalidOperationException($"Conflict (HTTP {statusCode}) still unresolved after {op.Attempts} attempts"));
                        return;
                    }

                    repository.Set(retry);
                    this.UpdateReceived?.Invoke(this, new SyncOperationResult(retry, SyncOperationState.Pending, statusCode, null));
                    try
                    {
                        // KeepLocal used to only mark the op Pending - nothing on Apple ever sent it again
                        await this.SendOperation(endpoint, retry).ConfigureAwait(false);
                    }
                    catch (Exception ex)
                    {
                        this.OnError(retry, 0, ex);
                    }
                    break;
            }
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error resolving sync conflict for op {id}", op.Identifier);
        }
        finally
        {
            this.EndDelegateDispatch();
        }
    }


    async Task DispatchConflictRemote(SyncEndpoint endpoint, string entityIdentifier, string remotePayload)
    {
        object? entity = null;
        try { entity = Shiny.Data.Sync.Infrastructure.SyncInboxProcessor.DeserializeEntity(endpoint, remotePayload); }
        catch (Exception ex) { logger.LogError(ex, "Failed to deserialize conflict remote payload for {key}", endpoint.Key); }

        var item = new SyncReceivedItem(endpoint.Key, endpoint.EntityType, entity, remotePayload, SyncVerb.Update, entityIdentifier);
        await services.RunDelegates<IDataSyncDelegate>(d => d.OnReceived(item), logger);
    }


    async Task<ConflictResolution> AskDelegateForConflict(SyncOperation op, string remotePayload)
    {
        foreach (var d in this.GetDelegates())
        {
            try
            {
                return await d.OnConflict(op, remotePayload).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogError(ex, "Delegate OnConflict threw - falling back to AcceptRemote");
            }
        }
        return ConflictResolution.AcceptRemote;
    }


    void OnCancel(SyncOperation op)
    {
        logger.OperationUpdate(op.Identifier, SyncOperationState.Canceled);
        this.TryDeleteTempFile(op.Identifier);
        repository.Remove(op);
        this.UpdateReceived?.Invoke(this, new SyncOperationResult(op, SyncOperationState.Canceled, null, null));
        this.RaiseActivity(new SyncEvent(SyncEventType.OutboxCanceled, op.EndpointKey, Operation: op));
        this.TryCompleteSession();
    }
}
