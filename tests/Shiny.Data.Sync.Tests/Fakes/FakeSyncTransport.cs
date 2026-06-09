using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Data.Sync.Infrastructure;

namespace Shiny.Data.Sync.Tests.Fakes;


/// <summary>
/// Scriptable <see cref="ISyncTransport"/> for unit tests. Configure responses per endpoint key
/// via <see cref="QueuePull"/> / <see cref="QueueTombstones"/> / <see cref="QueueSendResponse"/>
/// and inspect call history via <see cref="PullCalls"/> etc.
/// </summary>
public sealed class FakeSyncTransport : ISyncTransport
{
    readonly ConcurrentDictionary<string, Queue<Func<string?, SyncPullResult>>> pullQueue = new();
    readonly ConcurrentDictionary<string, Queue<Func<string?, SyncTombstoneResult>>> tombstoneQueue = new();
    readonly ConcurrentDictionary<string, Queue<SyncSendResult>> sendQueue = new();
    readonly ConcurrentDictionary<string, Queue<SyncBatchResult>> batchQueue = new();

    public List<(string Key, string? Cursor)> PullCalls { get; } = new();
    public List<(string Key, string? Cursor)> TombstoneCalls { get; } = new();
    public List<SyncOperation> SendCalls { get; } = new();
    public List<(SyncEndpoint Endpoint, IReadOnlyList<SyncOperation> Ops)> BatchCalls { get; } = new();


    public void QueuePull(string endpointKey, SyncPullResult result)
        => this.QueuePull(endpointKey, _ => result);


    public void QueuePull(string endpointKey, Func<string?, SyncPullResult> factory)
        => this.pullQueue.GetOrAdd(endpointKey, _ => new()).Enqueue(factory);


    public void QueueTombstones(string endpointKey, SyncTombstoneResult result)
        => this.tombstoneQueue.GetOrAdd(endpointKey, _ => new()).Enqueue(_ => result);


    public void QueueSendResponse(string endpointKey, SyncSendResult result)
        => this.sendQueue.GetOrAdd(endpointKey, _ => new()).Enqueue(result);


    public void QueueBatchResponse(string endpointKey, SyncBatchResult result)
        => this.batchQueue.GetOrAdd(endpointKey, _ => new()).Enqueue(result);


    public Task<SyncSendResult> Send(SyncEndpoint endpoint, SyncOperation operation, CancellationToken cancelToken)
    {
        this.SendCalls.Add(operation);
        if (this.sendQueue.TryGetValue(endpoint.Key, out var queue) && queue.Count > 0)
            return Task.FromResult(queue.Dequeue());
        return Task.FromResult(new SyncSendResult(200, "{}", false));
    }


    public Task<SyncPullResult> Pull(SyncEndpoint endpoint, string? cursor, CancellationToken cancelToken)
    {
        this.PullCalls.Add((endpoint.Key, cursor));
        if (this.pullQueue.TryGetValue(endpoint.Key, out var queue) && queue.Count > 0)
            return Task.FromResult(queue.Dequeue()(cursor));
        return Task.FromResult(new SyncPullResult(cursor, Array.Empty<SyncPullItem>()));
    }


    public Task<SyncBatchResult> SendBatch(SyncEndpoint endpoint, IReadOnlyList<SyncOperation> ops, CancellationToken cancelToken)
    {
        this.BatchCalls.Add((endpoint, ops));
        if (this.batchQueue.TryGetValue(endpoint.Key, out var queue) && queue.Count > 0)
            return Task.FromResult(queue.Dequeue());
        return Task.FromResult(new SyncBatchResult(200, Array.Empty<SyncBatchItemResult>()));
    }


    public Task<SyncTombstoneResult> FetchTombstones(SyncEndpoint endpoint, string? cursor, CancellationToken cancelToken)
    {
        this.TombstoneCalls.Add((endpoint.Key, cursor));
        if (this.tombstoneQueue.TryGetValue(endpoint.Key, out var queue) && queue.Count > 0)
            return Task.FromResult(queue.Dequeue()(cursor));
        return Task.FromResult(new SyncTombstoneResult(cursor, Array.Empty<string>()));
    }
}
