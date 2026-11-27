using System;
using System.Collections.Generic;
using System.Linq;
using System.Net;
using System.Net.Http;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shiny.Data.Sync.Infrastructure;
using Shiny.Data.Sync.Tests.Fakes;
using Xunit;

namespace Shiny.Data.Sync.Tests;


public class HttpClientDataSyncProcessTests
{
    static readonly TimeSpan LoopTimeout = TimeSpan.FromSeconds(10);


    static (HttpClientDataSyncProcess process, FakeSyncTransport transport, InMemoryRepository repo,
            RecordingDelegate del, SyncEndpoint endpoint)
        Build(Action<SyncEndpoint>? configure = null)
    {
        var registry = new SyncEndpointRegistry();
        registry.RegisterEndpoint<Item>("https://api/items", ep =>
        {
            ep.RetryBaseDelay = TimeSpan.FromMilliseconds(20);
            configure?.Invoke(ep);
        });
        var endpoint = registry.Get<Item>()!;
        var transport = new FakeSyncTransport();
        var repo = new InMemoryRepository();
        var del = new RecordingDelegate();
        var services = new ServiceCollection()
            .AddSingleton<IDataSyncDelegate>(del)
            .BuildServiceProvider();

        var process = new HttpClientDataSyncProcess(
            NullLogger<HttpClientDataSyncProcess>.Instance,
            repo,
            new WifiConnectivity(),
            transport,
            registry,
            services
        );
        return (process, transport, repo, del, endpoint);
    }


    static SyncOperation Op(SyncEndpoint endpoint, string id, string entityId, SyncVerb verb, string? payload, int order)
        => new(
            id,
            endpoint.Key,
            entityId,
            verb,
            payload,
            SyncOperationState.Pending,
            DateTimeOffset.UtcNow.AddMinutes(-10).AddSeconds(order),
            0,
            null
        );


    static async Task RunToCompletion(HttpClientDataSyncProcess process)
    {
        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        process.Run(() => tcs.TrySetResult());
        var finished = await Task.WhenAny(tcs.Task, Task.Delay(LoopTimeout));
        Assert.True(finished == tcs.Task, "outbox loop did not finish");
    }


    static HttpRequestException ServerUnavailable()
        => new("unavailable", null, HttpStatusCode.ServiceUnavailable);


    [Fact]
    public async Task Batch_TransientItemFailure_RetriesCoalescedPayload()
    {
        var (process, transport, repo, del, endpoint) = Build(ep => ep.Batch = true);
        repo.Set(Op(endpoint, "op1", "a", SyncVerb.Create, """{"identifier":"a","name":"v1"}""", 1));
        repo.Set(Op(endpoint, "op2", "a", SyncVerb.Update, """{"identifier":"a","name":"v2"}""", 2));
        repo.Set(Op(endpoint, "op3", "b", SyncVerb.Create, """{"identifier":"b","name":"b1"}""", 3));

        transport.QueueBatchResponse(endpoint.Key, new SyncBatchResult(200, new[]
        {
            new SyncBatchItemResult("op1", 503, null, false, "busy"),
            new SyncBatchItemResult("op3", 201, "{}", false, null)
        }));

        await RunToCompletion(process);

        // the 503'd coalesced op used to be removed along with its originals - the change was lost
        var resent = Assert.Single(transport.SendCalls);
        Assert.Equal("a", resent.EntityIdentifier);
        Assert.Equal(SyncVerb.Create, resent.Verb);
        Assert.Contains("v2", resent.Payload);

        Assert.Empty(repo.GetAll<SyncOperation>());
        Assert.Empty(del.Errors);
        Assert.Equal(2, del.Sent.Count);
    }


    [Fact]
    public async Task TransientFailure_HoldsBackLaterOpsForSameEntity()
    {
        var (process, transport, repo, _, endpoint) = Build();
        repo.Set(Op(endpoint, "op1", "a", SyncVerb.Create, """{"identifier":"a"}""", 1));
        repo.Set(Op(endpoint, "op2", "a", SyncVerb.Update, """{"identifier":"a","name":"x"}""", 2));
        transport.QueueSendFailure(endpoint.Key, ServerUnavailable());

        await RunToCompletion(process);

        // the update must not overtake the create that is waiting out its backoff
        var verbs = transport.SendCalls.Select(x => x.Verb).ToArray();
        Assert.Equal(new[] { SyncVerb.Create, SyncVerb.Create, SyncVerb.Update }, verbs);
        Assert.Empty(repo.GetAll<SyncOperation>());
    }


    [Fact]
    public async Task OpInBackoff_IsRetriedWithoutAnotherTrigger()
    {
        var (process, transport, repo, del, endpoint) = Build();
        repo.Set(Op(endpoint, "op1", "a", SyncVerb.Create, """{"identifier":"a"}""", 1));
        transport.QueueSendFailure(endpoint.Key, ServerUnavailable());

        await RunToCompletion(process);

        // the loop used to exit as soon as nothing was ready, stranding the op in its backoff window
        Assert.Equal(2, transport.SendCalls.Count);
        Assert.Single(del.Sent);
        Assert.Empty(repo.GetAll<SyncOperation>());
    }


    [Fact]
    public async Task ClientWinsConflict_StopsAfterMaxAttempts()
    {
        var (process, transport, repo, del, endpoint) = Build(ep =>
        {
            ep.DefaultConflictPolicy = ConflictPolicy.ClientWins;
            ep.MaxAttempts = 2;
        });
        repo.Set(Op(endpoint, "op1", "a", SyncVerb.Update, """{"identifier":"a"}""", 1));
        for (var i = 0; i < 5; i++)
            transport.QueueSendResponse(endpoint.Key, new SyncSendResult(409, """{"identifier":"a"}""", true));

        await RunToCompletion(process);

        Assert.Equal(2, transport.SendCalls.Count);
        var error = Assert.Single(del.Errors);
        Assert.Equal(409, error.StatusCode);
        Assert.Empty(repo.GetAll<SyncOperation>());
    }


    [Fact]
    public async Task AcceptRemoteConflict_ReceivedItemCarriesEntityIdentifier()
    {
        var (process, transport, repo, del, endpoint) = Build(ep => ep.DefaultConflictPolicy = ConflictPolicy.ServerWins);
        repo.Set(Op(endpoint, "op1", "a", SyncVerb.Update, """{"identifier":"a"}""", 1));
        transport.QueueSendResponse(endpoint.Key, new SyncSendResult(409, """{"identifier":"a","name":"server"}""", true));

        await RunToCompletion(process);

        Assert.Equal("a", del.Received.Single().EntityIdentifier);
    }


    static HttpRequestException Status(HttpStatusCode code)
        => new(code.ToString(), null, code);


    static List<SyncOperationResult> CaptureProgress(out Action unsubscribe)
    {
        var results = new List<SyncOperationResult>();
        EventHandler<SyncOperationResult> handler = (_, r) => { lock (results) results.Add(r); };
        HttpClientDataSyncProcess.ProgressOccurred += handler;
        unsubscribe = () => HttpClientDataSyncProcess.ProgressOccurred -= handler;
        return results;
    }


    [Theory]
    [InlineData(HttpStatusCode.NotFound)]
    [InlineData(HttpStatusCode.Gone)]
    public async Task Delete_MissingEntity_CompletesAsSent(HttpStatusCode code)
    {
        var (process, transport, repo, del, endpoint) = Build();
        repo.Set(Op(endpoint, "op1", "a", SyncVerb.Delete, null, 1));
        transport.QueueSendFailure(endpoint.Key, Status(code));

        var progress = CaptureProgress(out var unsubscribe);
        try
        {
            await RunToCompletion(process);
        }
        finally
        {
            unsubscribe();
        }

        Assert.Single(transport.SendCalls);
        Assert.Empty(del.Errors);
        Assert.Equal("op1", Assert.Single(del.Sent).Op.Identifier);
        Assert.Empty(repo.GetAll<SyncOperation>());

        List<SyncOperationResult> mine;
        lock (progress)
            mine = progress.Where(x => x.Operation.Identifier == "op1").ToList();
        var completed = Assert.Single(mine, x => x.State == SyncOperationState.Completed);
        Assert.Equal((int)code, completed.StatusCode);
    }


    [Fact]
    public async Task Delete_MissingEntity_FailsWhenOptionOff()
    {
        var (process, transport, repo, del, endpoint) = Build(ep => ep.TreatMissingAsDeleted = false);
        repo.Set(Op(endpoint, "op1", "a", SyncVerb.Delete, null, 1));
        transport.QueueSendFailure(endpoint.Key, Status(HttpStatusCode.NotFound));

        await RunToCompletion(process);

        Assert.Empty(del.Sent);
        Assert.Equal(404, Assert.Single(del.Errors).StatusCode);
        Assert.Empty(repo.GetAll<SyncOperation>());
    }


    [Fact]
    public async Task Update_NotFound_StillFails()
    {
        var (process, transport, repo, del, endpoint) = Build();
        repo.Set(Op(endpoint, "op1", "a", SyncVerb.Update, """{"identifier":"a"}""", 1));
        transport.QueueSendFailure(endpoint.Key, Status(HttpStatusCode.NotFound));

        await RunToCompletion(process);

        Assert.Empty(del.Sent);
        Assert.Equal(404, Assert.Single(del.Errors).StatusCode);
    }


    [Fact]
    public async Task Batch_DeleteItemNotFound_CompletesAsSent()
    {
        var (process, transport, repo, del, endpoint) = Build(ep => ep.Batch = true);
        repo.Set(Op(endpoint, "op1", "a", SyncVerb.Delete, null, 1));
        repo.Set(Op(endpoint, "op2", "b", SyncVerb.Update, """{"identifier":"b"}""", 2));
        transport.QueueBatchResponse(endpoint.Key, new SyncBatchResult(200, new[]
        {
            new SyncBatchItemResult("op1", 404, null, false, "not found"),
            new SyncBatchItemResult("op2", 200, "{}", false, null)
        }));

        await RunToCompletion(process);

        Assert.Empty(del.Errors);
        Assert.Equal(new[] { "op1", "op2" }, del.Sent.Select(x => x.Op.Identifier).OrderBy(x => x).ToArray());
        Assert.Empty(repo.GetAll<SyncOperation>());
    }


    [Fact]
    public async Task Batch_UnsentCreateThenDelete_SendsNothing()
    {
        var (process, transport, repo, del, endpoint) = Build(ep => ep.Batch = true);
        repo.Set(Op(endpoint, "op1", "a", SyncVerb.Create, """{"identifier":"a"}""", 1));
        repo.Set(Op(endpoint, "op2", "a", SyncVerb.Delete, null, 2));

        var progress = CaptureProgress(out var unsubscribe);
        try
        {
            await RunToCompletion(process);
        }
        finally
        {
            unsubscribe();
        }

        Assert.Empty(transport.BatchCalls);
        Assert.Empty(transport.SendCalls);
        Assert.Empty(del.Sent);
        Assert.Empty(del.Errors);
        Assert.Empty(repo.GetAll<SyncOperation>());

        List<SyncOperationResult> mine;
        lock (progress)
            mine = progress.Where(x => x.Operation.Identifier is "op1" or "op2").ToList();
        Assert.Equal(2, mine.Count);
        Assert.All(mine, x => Assert.Equal(SyncOperationState.Canceled, x.State));
    }


    [Fact]
    public async Task Batch_UnsentPairDropped_OtherOpsStillSent()
    {
        var (process, transport, repo, del, endpoint) = Build(ep => ep.Batch = true);
        repo.Set(Op(endpoint, "op1", "a", SyncVerb.Create, """{"identifier":"a"}""", 1));
        repo.Set(Op(endpoint, "op2", "a", SyncVerb.Delete, null, 2));
        repo.Set(Op(endpoint, "op3", "b", SyncVerb.Create, """{"identifier":"b"}""", 3));
        transport.QueueBatchResponse(endpoint.Key, new SyncBatchResult(200, new[]
        {
            new SyncBatchItemResult("op3", 201, "{}", false, null)
        }));

        await RunToCompletion(process);

        var batch = Assert.Single(transport.BatchCalls);
        Assert.Equal("b", Assert.Single(batch.Ops).EntityIdentifier);
        Assert.Equal("op3", Assert.Single(del.Sent).Op.Identifier);
        Assert.Empty(repo.GetAll<SyncOperation>());
    }


    [Fact]
    public async Task Batch_AttemptedCreateThenDelete_SendsDelete()
    {
        var (process, transport, repo, del, endpoint) = Build(ep => ep.Batch = true);
        repo.Set(Op(endpoint, "op1", "a", SyncVerb.Create, """{"identifier":"a"}""", 1) with { Attempts = 1 });
        repo.Set(Op(endpoint, "op2", "a", SyncVerb.Delete, null, 2));
        transport.QueueBatchResponse(endpoint.Key, new SyncBatchResult(200, new[]
        {
            new SyncBatchItemResult("op1", 404, null, false, "not found")
        }));

        await RunToCompletion(process);

        var batch = Assert.Single(transport.BatchCalls);
        Assert.Equal(SyncVerb.Delete, Assert.Single(batch.Ops).Verb);
        Assert.Empty(del.Errors);
        Assert.Single(del.Sent);
        Assert.Empty(repo.GetAll<SyncOperation>());
    }


    [Fact]
    public async Task Batch_SendThrows_CreateNoLongerCountsAsUnsent()
    {
        var (process, transport, repo, _, endpoint) = Build(ep => ep.Batch = true);
        repo.Set(Op(endpoint, "op1", "a", SyncVerb.Create, """{"identifier":"a"}""", 1));
        repo.Set(Op(endpoint, "op2", "b", SyncVerb.Create, """{"identifier":"b"}""", 2));
        transport.QueueBatchFailure(endpoint.Key, new TimeoutException("timed out"));
        transport.QueueBatchResponse(endpoint.Key, new SyncBatchResult(200, new[]
        {
            new SyncBatchItemResult("op1", 201, "{}", false, null),
            new SyncBatchItemResult("op2", 201, "{}", false, null)
        }));

        await RunToCompletion(process);

        // the batch that threw may have reached the server - its ops carry the attempt
        Assert.Equal(2, transport.BatchCalls.Count);
        Assert.All(transport.BatchCalls[1].Ops, op => Assert.Equal(2, op.Attempts));
    }


    sealed class WifiConnectivity : Shiny.Net.IConnectivity
    {
        public event EventHandler? Changed { add { } remove { } }
        public Shiny.Net.ConnectionTypes ConnectionTypes => Shiny.Net.ConnectionTypes.Wifi;
        public Shiny.Net.NetworkAccess Access => Shiny.Net.NetworkAccess.Internet;
    }
}
