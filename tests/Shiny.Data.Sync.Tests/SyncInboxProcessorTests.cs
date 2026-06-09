using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging.Abstractions;
using Shiny.Data.Sync.Infrastructure;
using Shiny.Data.Sync.Tests.Fakes;
using Xunit;

namespace Shiny.Data.Sync.Tests;


public class SyncInboxProcessorTests
{
    static (SyncInboxProcessor processor, SyncEndpointRegistry registry, FakeSyncTransport transport,
            InMemoryRepository repo, RecordingDelegate del, SyncEndpoint endpoint)
        BuildFixture(Action<SyncEndpoint>? configure = null)
    {
        var registry = new SyncEndpointRegistry();
        registry.RegisterEndpoint<Item>("https://api/items", configure);
        var endpoint = registry.Get<Item>()!;
        var transport = new FakeSyncTransport();
        var repo = new InMemoryRepository();
        var del = new RecordingDelegate();
        var processor = new SyncInboxProcessor(
            NullLogger<SyncInboxProcessor>.Instance,
            repo,
            transport,
            registry,
            new[] { del }
        );
        return (processor, registry, transport, repo, del, endpoint);
    }


    static SyncPullItem MakePull(string id, SyncVerb verb, string payload)
        => new(id, verb, payload);


    [Fact]
    public async Task Pull_DispatchesEveryItemToDelegate()
    {
        var (proc, _, transport, _, del, endpoint) = BuildFixture();
        transport.QueuePull(endpoint.Key, new SyncPullResult(
            "cur-1",
            new[]
            {
                MakePull("a", SyncVerb.Create, """{"identifier":"a","name":"Alpha"}"""),
                MakePull("b", SyncVerb.Update, """{"identifier":"b","name":"Beta"}""")
            }
        ));

        await proc.PullEndpoint(endpoint);

        Assert.Equal(2, del.Received.Count);
        var receivedIds = del.Received.Select(r => ((Item)r.Entity!).Identifier).OrderBy(s => s).ToArray();
        Assert.Equal(new[] { "a", "b" }, receivedIds);
    }


    [Fact]
    public async Task Pull_PersistsCursorAndLastPulledAt()
    {
        var (proc, _, transport, repo, _, endpoint) = BuildFixture();
        transport.QueuePull(endpoint.Key, new SyncPullResult("new-cursor", Array.Empty<SyncPullItem>()));

        await proc.PullEndpoint(endpoint);

        var cursor = repo.Get<SyncCursor>(endpoint.Key);
        Assert.NotNull(cursor);
        Assert.Equal("new-cursor", cursor!.Cursor);
    }


    [Fact]
    public async Task Pull_HasMore_LoopsUntilExhausted()
    {
        var (proc, _, transport, _, del, endpoint) = BuildFixture();
        transport.QueuePull(endpoint.Key, new SyncPullResult(
            "cur-1",
            new[] { MakePull("a", SyncVerb.Create, """{"identifier":"a"}""") },
            HasMore: true
        ));
        transport.QueuePull(endpoint.Key, new SyncPullResult(
            "cur-2",
            new[] { MakePull("b", SyncVerb.Create, """{"identifier":"b"}""") },
            HasMore: true
        ));
        transport.QueuePull(endpoint.Key, new SyncPullResult(
            "cur-3",
            new[] { MakePull("c", SyncVerb.Create, """{"identifier":"c"}""") }
        ));

        await proc.PullEndpoint(endpoint);

        Assert.Equal(3, transport.PullCalls.Count);
        Assert.Equal(3, del.Received.Count);
    }


    [Fact]
    public async Task Pull_NotModified_KeepsCursor_RaisesCompleted()
    {
        var (proc, _, transport, repo, del, endpoint) = BuildFixture();
        repo.Set(new SyncCursor(endpoint.Key, "existing", DateTimeOffset.UtcNow.AddMinutes(-10)));

        transport.QueuePull(endpoint.Key, new SyncPullResult("existing", Array.Empty<SyncPullItem>(), NotModified: true));

        SyncPullCompletion? completion = null;
        proc.PullCompleted += (s, c) => completion = c;

        await proc.PullEndpoint(endpoint);

        Assert.Empty(del.Received);
        Assert.Equal("existing", repo.Get<SyncCursor>(endpoint.Key)!.Cursor);
        Assert.NotNull(completion);
        Assert.Equal(0, completion!.ItemsReceived);
        Assert.Null(completion.Error);
    }


    [Fact]
    public async Task Pull_TransportThrows_RaisesCompletedWithError()
    {
        var (proc, _, transport, _, _, endpoint) = BuildFixture();
        transport.QueuePull(endpoint.Key, _ => throw new InvalidOperationException("boom"));

        SyncPullCompletion? completion = null;
        proc.PullCompleted += (s, c) => completion = c;

        await proc.PullEndpoint(endpoint);

        Assert.NotNull(completion);
        Assert.IsType<InvalidOperationException>(completion!.Error);
    }


    [Fact]
    public async Task Pull_MinPullInterval_BlocksScheduled_AllowsForce()
    {
        var (proc, _, transport, repo, _, endpoint) = BuildFixture(ep =>
            ep.MinPullInterval = TimeSpan.FromMinutes(5));
        repo.Set(new SyncCursor(endpoint.Key, "x", DateTimeOffset.UtcNow));

        await proc.PullEndpoint(endpoint, force: false);
        Assert.Empty(transport.PullCalls);

        transport.QueuePull(endpoint.Key, new SyncPullResult("x", Array.Empty<SyncPullItem>()));
        await proc.PullEndpoint(endpoint, force: true);
        Assert.Single(transport.PullCalls);
    }


    [Fact]
    public async Task Pull_PushOnlyEndpoint_SilentlySkips()
    {
        var (proc, _, transport, _, del, endpoint) = BuildFixture(ep => ep.Direction = SyncDirection.PushOnly);

        await proc.PullEndpoint(endpoint);

        Assert.Empty(transport.PullCalls);
        Assert.Empty(del.Received);
    }


    [Fact]
    public async Task SoftDeletePredicate_FlipsVerbToDelete()
    {
        var (proc, _, transport, _, del, endpoint) = BuildFixture(ep =>
            ep.SoftDeletePredicate = entity => entity is Item it && it.IsArchived);

        transport.QueuePull(endpoint.Key, new SyncPullResult(
            "c",
            new[] { MakePull("a", SyncVerb.Update, """{"identifier":"a","name":"X","isArchived":true}""") }
        ));

        await proc.PullEndpoint(endpoint);

        var received = del.Received.Single();
        Assert.Equal(SyncVerb.Delete, received.Verb);
        Assert.NotNull(received.Entity); // entity stays populated even on flip
    }


    [Fact]
    public async Task ExpiryPredicate_FlipsVerbToDelete()
    {
        var (proc, _, transport, _, del, endpoint) = BuildFixture(ep =>
            ep.ExpiryPredicate = entity => entity is Item it && it.Name == "expired");

        transport.QueuePull(endpoint.Key, new SyncPullResult(
            "c",
            new[] { MakePull("a", SyncVerb.Update, """{"identifier":"a","name":"expired"}""") }
        ));

        await proc.PullEndpoint(endpoint);

        Assert.Equal(SyncVerb.Delete, del.Received.Single().Verb);
    }


    [Fact]
    public async Task DeleteVerb_NotAffectedByPredicate()
    {
        var (proc, _, transport, _, del, endpoint) = BuildFixture(ep =>
            ep.SoftDeletePredicate = _ => true);

        transport.QueuePull(endpoint.Key, new SyncPullResult(
            "c",
            new[] { MakePull("a", SyncVerb.Delete, "null") }
        ));

        await proc.PullEndpoint(endpoint);

        Assert.Equal(SyncVerb.Delete, del.Received.Single().Verb);
    }


    [Fact]
    public async Task TombstoneUrl_TriggersTombstoneFetch_DispatchesDeletes()
    {
        var (proc, _, transport, _, del, endpoint) = BuildFixture(ep =>
            ep.TombstoneUrl = "https://api/tombstones");

        transport.QueuePull(endpoint.Key, new SyncPullResult("cur", Array.Empty<SyncPullItem>()));
        transport.QueueTombstones(endpoint.Key, new SyncTombstoneResult("tcur", new[] { "x", "y" }));

        await proc.PullEndpoint(endpoint);

        Assert.Single(transport.TombstoneCalls);
        Assert.Equal(2, del.Received.Count);
        Assert.All(del.Received, r => Assert.Equal(SyncVerb.Delete, r.Verb));
    }


    [Fact]
    public async Task NoTombstoneUrl_NoTombstoneFetch()
    {
        var (proc, _, transport, _, _, endpoint) = BuildFixture();
        transport.QueuePull(endpoint.Key, new SyncPullResult("cur", Array.Empty<SyncPullItem>()));

        await proc.PullEndpoint(endpoint);

        Assert.Empty(transport.TombstoneCalls);
    }


    [Fact]
    public async Task PullFailed_SkipsTombstoneFetch()
    {
        var (proc, _, transport, _, _, endpoint) = BuildFixture(ep => ep.TombstoneUrl = "https://api/tombstones");
        transport.QueuePull(endpoint.Key, _ => throw new InvalidOperationException("boom"));

        await proc.PullEndpoint(endpoint);

        Assert.Empty(transport.TombstoneCalls);
    }


    [Fact]
    public async Task Activity_FiresStartItemCompleted()
    {
        var (proc, _, transport, _, _, endpoint) = BuildFixture();
        transport.QueuePull(endpoint.Key, new SyncPullResult(
            "c",
            new[] { MakePull("a", SyncVerb.Create, """{"identifier":"a"}""") }
        ));

        var events = new List<SyncEvent>();
        proc.Activity += (s, e) => events.Add(e);

        await proc.PullEndpoint(endpoint);

        Assert.Contains(events, e => e.Type == SyncEventType.InboxPullStarted);
        Assert.Contains(events, e => e.Type == SyncEventType.InboxItemReceived);
        Assert.Contains(events, e => e.Type == SyncEventType.InboxPullCompleted);
    }


    [Fact]
    public async Task Activity_OnFailure_FiresPullFailed()
    {
        var (proc, _, transport, _, _, endpoint) = BuildFixture();
        transport.QueuePull(endpoint.Key, _ => throw new InvalidOperationException("boom"));

        var events = new List<SyncEvent>();
        proc.Activity += (s, e) => events.Add(e);

        await proc.PullEndpoint(endpoint);

        Assert.Contains(events, e => e.Type == SyncEventType.InboxPullFailed);
    }


    [Fact]
    public async Task TombstonesApplied_EventFired()
    {
        var (proc, _, transport, _, _, endpoint) = BuildFixture(ep => ep.TombstoneUrl = "https://api/tombstones");
        transport.QueuePull(endpoint.Key, new SyncPullResult("c", Array.Empty<SyncPullItem>()));
        transport.QueueTombstones(endpoint.Key, new SyncTombstoneResult("t", new[] { "x" }));

        var events = new List<SyncEvent>();
        proc.Activity += (s, e) => events.Add(e);

        await proc.PullEndpoint(endpoint);

        Assert.Contains(events, e => e.Type == SyncEventType.TombstonesApplied && e.ItemCount == 1);
    }


    [Fact]
    public async Task PullAll_SkipsPushOnly_PullsRest()
    {
        var registry = new SyncEndpointRegistry();
        registry.RegisterEndpoint<Item>("https://api/items");
        var transport = new FakeSyncTransport();
        var repo = new InMemoryRepository();
        var del = new RecordingDelegate();
        var proc = new SyncInboxProcessor(NullLogger<SyncInboxProcessor>.Instance, repo, transport, registry, new[] { del });
        transport.QueuePull(registry.Get<Item>()!.Key, new SyncPullResult("c", Array.Empty<SyncPullItem>()));

        await proc.PullAll();

        Assert.Single(transport.PullCalls);
    }
}
