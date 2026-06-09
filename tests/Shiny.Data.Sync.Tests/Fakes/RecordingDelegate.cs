using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shiny.Data.Sync.Tests.Fakes;


/// <summary>
/// Records every callback the engine emits and replays scripted conflict resolutions.
/// </summary>
public sealed class RecordingDelegate : IDataSyncDelegate
{
    public ConcurrentBag<(SyncOperation Op, string? ResponseBody)> Sent { get; } = new();
    public ConcurrentBag<(SyncOperation Op, int StatusCode, Exception Ex)> Errors { get; } = new();
    public ConcurrentBag<SyncReceivedItem> Received { get; } = new();
    public Queue<ConflictResolution> ConflictResolutions { get; } = new();

    public Task OnSent(SyncOperation operation, string? responseBody)
    {
        this.Sent.Add((operation, responseBody));
        return Task.CompletedTask;
    }

    public Task OnError(SyncOperation operation, int statusCode, Exception ex)
    {
        this.Errors.Add((operation, statusCode, ex));
        return Task.CompletedTask;
    }

    public Task OnReceived(SyncReceivedItem item)
    {
        this.Received.Add(item);
        return Task.CompletedTask;
    }

    public Task<ConflictResolution> OnConflict(SyncOperation operation, string remotePayload)
    {
        var resolution = this.ConflictResolutions.Count > 0 ? this.ConflictResolutions.Dequeue() : ConflictResolution.AcceptRemote;
        return Task.FromResult(resolution);
    }
}
