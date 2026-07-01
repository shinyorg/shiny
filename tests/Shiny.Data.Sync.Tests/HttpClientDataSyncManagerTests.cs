using System;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shiny.Data.Sync.Infrastructure;
using Shiny.Data.Sync.Tests.Fakes;
using Shiny.Extensions.Stores.Repositories;
using Xunit;

namespace Shiny.Data.Sync.Tests;


public class HttpClientDataSyncManagerTests
{
    static (HttpClientDataSyncManager mgr, FakeSyncTransport transport, InMemoryRepository repo,
            RecordingDelegate del, SyncEndpoint endpoint)
        Build(Action<SyncEndpoint>? configure = null)
    {
        var registry = new SyncEndpointRegistry();
        registry.RegisterEndpoint<Item>("https://api/items", configure);
        var endpoint = registry.Get<Item>()!;
        var transport = new FakeSyncTransport();
        var repo = new InMemoryRepository();
        var del = new RecordingDelegate();
        var services = new ServiceCollection()
            .AddSingleton<IDataSyncDelegate>(del)
            .BuildServiceProvider();
        var inbox = new SyncInboxProcessor(NullLogger<SyncInboxProcessor>.Instance, repo, transport, registry, services);
        var process = new HttpClientDataSyncProcess(
            NullLogger<HttpClientDataSyncProcess>.Instance,
            repo,
            new StubConnectivity(),
            transport,
            registry,
            services
        );
        var mgr = new HttpClientDataSyncManager(process, inbox, NullLogger<HttpClientDataSyncManager>.Instance, repo, registry);
        return (mgr, transport, repo, del, endpoint);
    }


    [Fact]
    public async Task Queue_InsertsOpAndFiresActivity()
    {
        var (mgr, _, repo, _, endpoint) = Build();
        SyncEvent? evt = null;
        mgr.Activity += (s, e) => { if (e.Type == SyncEventType.OutboxQueued) evt = e; };

        var op = await mgr.Queue(SyncVerb.Create, new Item("a", "Alice"));

        Assert.NotNull(repo.Get<SyncOperation>(op.Identifier));
        Assert.Equal(endpoint.Key, op.EndpointKey);
        Assert.NotNull(evt);
        Assert.Equal(op.Identifier, evt!.Operation!.Identifier);
    }


    [Fact]
    public async Task Queue_OnPullOnlyEndpoint_Throws()
    {
        var (mgr, _, _, _, _) = Build(ep => ep.Direction = SyncDirection.PullOnly);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(
            () => mgr.Queue(SyncVerb.Create, new Item("a")));

        Assert.Contains("PullOnly", ex.Message);
    }


    [Fact]
    public async Task Queue_Delete_HasNullPayload()
    {
        var (mgr, _, _, _, _) = Build();
        var op = await mgr.Queue(SyncVerb.Delete, new Item("a"));
        Assert.Equal(SyncVerb.Delete, op.Verb);
        Assert.Null(op.Payload);
    }


    [Fact]
    public async Task Queue_CreateUpdate_SerializesPayload()
    {
        var (mgr, _, _, _, _) = Build();
        var op = await mgr.Queue(SyncVerb.Create, new Item("a", "Alpha"));
        Assert.Contains("Alpha", op.Payload);
    }


    [Fact]
    public async Task PullNow_OnPushOnlyEndpoint_Throws()
    {
        var (mgr, _, _, _, _) = Build(ep => ep.Direction = SyncDirection.PushOnly);

        var ex = await Assert.ThrowsAsync<InvalidOperationException>(() => mgr.PullNow<Item>());
        Assert.Contains("PushOnly", ex.Message);
    }


    [Fact]
    public async Task PullNow_OnUnregisteredEntity_Throws()
    {
        var (mgr, _, _, _, _) = Build();
        await Assert.ThrowsAsync<InvalidOperationException>(() => mgr.PullNow<NotRegistered>());
    }


    [Fact]
    public async Task PullNow_BypassesMinPullInterval()
    {
        var (mgr, transport, repo, _, endpoint) = Build(ep => ep.MinPullInterval = TimeSpan.FromHours(1));
        repo.Set(new SyncCursor(endpoint.Key, "x", DateTimeOffset.UtcNow));

        transport.QueuePull(endpoint.Key, new SyncPullResult("x", Array.Empty<SyncPullItem>()));
        await mgr.PullNow<Item>();

        Assert.Single(transport.PullCalls);
    }


    [Fact]
    public async Task PullAll_RespectsMinPullInterval()
    {
        var (mgr, transport, repo, _, endpoint) = Build(ep => ep.MinPullInterval = TimeSpan.FromHours(1));
        repo.Set(new SyncCursor(endpoint.Key, "x", DateTimeOffset.UtcNow));

        await mgr.PullAll();

        Assert.Empty(transport.PullCalls);
    }


    [Fact]
    public async Task CancelAll_ClearsRepository()
    {
        var (mgr, _, repo, _, _) = Build();
        await mgr.Queue(SyncVerb.Create, new Item("a"));
        await mgr.Queue(SyncVerb.Create, new Item("b"));

        await mgr.CancelAll();

        Assert.Empty(repo.GetAll<SyncOperation>());
    }


    [Fact]
    public async Task CancelAllT_OnlyRemovesMatchingEndpoint()
    {
        var (mgr, _, repo, _, _) = Build();
        await mgr.Queue(SyncVerb.Create, new Item("a"));

        await mgr.CancelAll<Item>();

        Assert.DoesNotContain(repo.GetAll<SyncOperation>(), o => o.EndpointKey == typeof(Item).FullName);
    }


    [Fact]
    public async Task GetPending_ReturnsAllQueuedOps()
    {
        var (mgr, _, _, _, _) = Build();
        await mgr.Queue(SyncVerb.Create, new Item("a"));
        await mgr.Queue(SyncVerb.Update, new Item("a", "renamed"));

        var pending = await mgr.GetPending();

        Assert.Equal(2, pending.Count);
    }


    sealed class StubConnectivity : Shiny.Net.IConnectivity
    {
        public event EventHandler? Changed;
        public Shiny.Net.ConnectionTypes ConnectionTypes => Shiny.Net.ConnectionTypes.Wifi;
        public Shiny.Net.NetworkAccess Access => Shiny.Net.NetworkAccess.Internet;
        void RaiseChanged() => this.Changed?.Invoke(this, EventArgs.Empty);
    }
}
