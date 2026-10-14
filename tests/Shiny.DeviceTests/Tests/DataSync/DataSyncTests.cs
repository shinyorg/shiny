using System.Net;
using System.Net.Http.Json;
using System.Text;
using Shiny.Data.Sync;

namespace Shiny.DeviceTests.Tests;


/// <summary>
/// Outbox pushes, inbox pulls, tombstones, retries and conflicts against the runner's in-memory sync server
/// (DataSyncFixture). iOS / Mac Catalyst send through the background NSURLSession; Android through the
/// foreground service + HttpClient loop.
/// </summary>
public class DataSyncTests(
    IDataSyncManager manager,
    DataSyncTestState state
) : DeviceTestSuite
{
    public override string Name => "DataSync";

    static readonly string EndpointKey = typeof(SyncNote).FullName!;


    public override async Task SetupAsync(TestContext context)
    {
        context.RequireCapability(HostCapability.Server);
        context.SkipUnless(
            String.Equals(context.Host.BaseUrl, DataSyncRegistration.RunnerBaseUrl, StringComparison.OrdinalIgnoreCase),
            $"Sync endpoints are registered against {DataSyncRegistration.RunnerBaseUrl} but the runner is at {context.Host.BaseUrl} (physical device)"
        );

        await manager.CancelAll<SyncNote>();
        await Post(context, "sync/test/reset");
    }


    public override async Task TeardownAsync(TestContext context)
    {
        state.ConflictResolver = (_, _) => ConflictResolution.AcceptRemote;
        await manager.CancelAll<SyncNote>();
    }


    protected override void Define()
    {
        this.Test("Push_Create_ReachesServer", async ctx =>
        {
            var note = NewNote("created on device");
            await manager.Queue(SyncVerb.Create, note);

            var sent = await this.WaitForSent(ctx, note.Identifier);
            Assert.Equal(SyncVerb.Create, sent.Operation.Verb);
            Assert.Contains("created on device", sent.ResponseBody ?? "");

            var write = Assert.Single(await GetWrites(ctx, note.Identifier));
            Assert.Equal("POST", write.Method);
            Assert.Equal(note, await GetServerNote(ctx, note.Identifier));
            await this.AssertNotPending(note.Identifier);
        });


        this.Test("Push_Update_UsesPutWithIdentifier", async ctx =>
        {
            var note = NewNote("v1");
            await PutServerNote(ctx, note);

            var updated = note with { Title = "v2", Revision = 2 };
            await manager.Queue(SyncVerb.Update, updated);
            await this.WaitForSent(ctx, note.Identifier);

            var write = Assert.Single(await GetWrites(ctx, note.Identifier));
            Assert.Equal("PUT", write.Method);
            Assert.Equal(updated, await GetServerNote(ctx, note.Identifier));
        });


        this.Test("Push_Delete_RemovesServerEntity", async ctx =>
        {
            var note = NewNote("to delete");
            await PutServerNote(ctx, note);

            await manager.Queue(SyncVerb.Delete, note);
            await this.WaitForSent(ctx, note.Identifier);

            var write = Assert.Single(await GetWrites(ctx, note.Identifier));
            Assert.Equal("DELETE", write.Method);
            Assert.Null(await GetServerNote(ctx, note.Identifier));
        });


        this.Test("Push_TransientFailure_RetriesThenSucceeds", async ctx =>
        {
            var note = NewNote("retry me");
            await Post(ctx, $"sync/test/fail/{note.Identifier}?status=503&count=1");

            await manager.Queue(SyncVerb.Create, note);
            await this.WaitForSent(ctx, note.Identifier, TimeSpan.FromSeconds(30));

            var writes = await GetWrites(ctx, note.Identifier);
            Assert.Equal(2, writes.Count);
            Assert.Equal(503, writes[0].Status);
            Assert.Equal(201, writes[1].Status);
            Assert.Equal(note, await GetServerNote(ctx, note.Identifier));
        });


        this.Test("Push_PermanentFailure_ReportsError", async ctx =>
        {
            var note = NewNote("rejected");
            await Post(ctx, $"sync/test/fail/{note.Identifier}?status=400&count=10");

            await manager.Queue(SyncVerb.Create, note);
            var failed = await ctx.Events.WaitAsync<SyncFailed>(TestDataSyncDelegate.ErrorTopic, TimeSpan.FromSeconds(30), ctx.CancellationToken, x => x.Operation.EntityIdentifier == note.Identifier);

            Assert.Equal(400, failed.StatusCode);
            Assert.Single(await GetWrites(ctx, note.Identifier));  // 4xx is not retried
            await this.AssertNotPending(note.Identifier);
        });


        // endpoint MaxAttempts = 3: a 503 that never clears must reach OnError after three sends, not retry forever
        this.Test("Push_TransientFailureExhausted_ReportsErrorAfterMaxAttempts", async ctx =>
        {
            var note = NewNote("never accepted");
            await Post(ctx, $"sync/test/fail/{note.Identifier}?status=503&count=99");

            await manager.Queue(SyncVerb.Create, note);
            try
            {
                var failed = await ctx.Events.WaitAsync<SyncFailed>(TestDataSyncDelegate.ErrorTopic, TimeSpan.FromSeconds(45), ctx.CancellationToken, x => x.Operation.EntityIdentifier == note.Identifier);
                Assert.Equal(503, failed.StatusCode);
                Assert.Equal(3, (await GetWrites(ctx, note.Identifier)).Count);
                await this.AssertNotPending(note.Identifier);
            }
            catch (TimeoutException)
            {
                var writes = await GetWrites(ctx, note.Identifier);
                throw new Xunit.Sdk.XunitException($"OnError never fired - the server saw {writes.Count} attempt(s) and the op is still queued");
            }
        }, TimeSpan.FromSeconds(60));


        this.Test("Pull_ServerChanges_DeliveredToDelegate", async ctx =>
        {
            var a = NewNote("server a");
            var b = NewNote("server b");
            await Seed(ctx, a);
            await Seed(ctx, b);

            var completion = await this.PullAndWait(ctx);
            ctx.Log($"pull received {completion.ItemsReceived}, cursor {completion.Cursor}");

            var notes = ReceivedNotes(ctx);
            Assert.Contains(a, notes);
            Assert.Contains(b, notes);
            Assert.All(
                ctx.Events.Get<SyncReceivedItem>(TestDataSyncDelegate.ReceivedTopic).Where(x => x.Entity is SyncNote n && (n == a || n == b)),
                x => Assert.Equal(SyncVerb.Create, x.Verb)
            );
            Assert.NotNull(completion.Cursor);
        });


        this.Test("Pull_Incremental_OnlyFetchesNewChanges", async ctx =>
        {
            var first = NewNote("first");
            await Seed(ctx, first);
            await this.PullAndWait(ctx);
            Assert.Contains(first, ReceivedNotes(ctx));

            ctx.Events.Clear(TestDataSyncDelegate.ReceivedTopic);
            var second = NewNote("second");
            await Seed(ctx, second);
            await this.PullAndWait(ctx);

            var notes = ReceivedNotes(ctx);
            Assert.Contains(second, notes);
            Assert.DoesNotContain(first, notes);

            var pulls = await GetPulls(ctx);
            ctx.Log($"cursors sent: {String.Join(", ", pulls.Select(x => x ?? "<none>"))}");
            Assert.NotNull(pulls[^1]);
        });


        this.Test("Pull_HasMore_DrainsEveryPage", async ctx =>
        {
            await this.PullAndWait(ctx);  // start from an empty feed position
            ctx.Events.Clear(TestDataSyncDelegate.ReceivedTopic);

            await Post(ctx, "sync/test/pagesize/2");
            try
            {
                var notes = Enumerable.Range(1, 5).Select(x => NewNote($"page {x}")).ToList();
                foreach (var note in notes)
                    await Seed(ctx, note);

                await manager.PullNow<SyncNote>(ctx.CancellationToken);
                await ctx.WaitUntilAsync(() => notes.All(ReceivedNotes(ctx).Contains), TimeSpan.FromSeconds(30), "all five notes across three pages");

                var pulls = await GetPulls(ctx);
                ctx.Log($"{pulls.Count} pull requests");
            }
            finally
            {
                await Post(ctx, "sync/test/pagesize/100");
            }
        });


        this.Test("Pull_ServerDelete_DeliveredAsDelete", async ctx =>
        {
            var note = NewNote("deleted on server");
            await Seed(ctx, note);
            await Seed(ctx, note, "Delete");

            await this.PullAndWait(ctx);
            var item = await ctx.Events.WaitAsync<SyncReceivedItem>(
                TestDataSyncDelegate.ReceivedTopic,
                TimeSpan.FromSeconds(10),
                ctx.CancellationToken,
                x => x.Verb == SyncVerb.Delete && x.Entity is SyncNote n && n.Identifier == note.Identifier
            );
            Assert.Equal(EndpointKey, item.EndpointKey);
        });


        this.Test("Tombstones_DispatchDelete", async ctx =>
        {
            var note = NewNote("tombstoned");
            await Seed(ctx, note);
            await this.PullAndWait(ctx);
            ctx.Events.Clear(TestDataSyncDelegate.ReceivedTopic);

            await Post(ctx, $"sync/test/tombstone/{note.Identifier}");
            await manager.PullNow<SyncNote>(ctx.CancellationToken);

            // the tombstone fetch follows the main pull, so wait on the delete itself
            var item = await ctx.Events.WaitAsync<SyncReceivedItem>(TestDataSyncDelegate.ReceivedTopic, TimeSpan.FromSeconds(30), ctx.CancellationToken, x => x.Verb == SyncVerb.Delete);
            Assert.Equal(EndpointKey, item.EndpointKey);
        });


        // a delete from the tombstone stream is only actionable if the delegate can tell which entity it was
        this.Test("Tombstones_IdentifyDeletedEntity", async ctx =>
        {
            var note = NewNote("tombstoned with id");
            await Seed(ctx, note);
            await this.PullAndWait(ctx);
            ctx.Events.Clear(TestDataSyncDelegate.ReceivedTopic);

            await Post(ctx, $"sync/test/tombstone/{note.Identifier}");
            await manager.PullNow<SyncNote>(ctx.CancellationToken);

            var item = await ctx.Events.WaitAsync<SyncReceivedItem>(TestDataSyncDelegate.ReceivedTopic, TimeSpan.FromSeconds(30), ctx.CancellationToken, x => x.Verb == SyncVerb.Delete);
            ctx.Log($"tombstone item: entity={item.Entity ?? "<null>"} raw={item.RawPayload}");
            Assert.True(
                (item.Entity as ISyncEntity)?.Identifier == note.Identifier || item.RawPayload.Contains(note.Identifier, StringComparison.Ordinal),
                $"The tombstone delete for {note.Identifier} carries no identifier (Entity={item.Entity ?? "null"}, RawPayload={item.RawPayload})"
            );
        });


        this.Test("Conflict_AcceptRemote_DeliversServerState", async ctx =>
        {
            var remote = NewNote("server copy");
            await PutServerNote(ctx, remote);
            await Post(ctx, $"sync/test/conflict/{remote.Identifier}?count=1");
            state.ConflictResolver = (_, _) => ConflictResolution.AcceptRemote;

            await manager.Queue(SyncVerb.Update, remote with { Title = "device copy" });

            var conflict = await ctx.Events.WaitAsync<SyncConflict>(TestDataSyncDelegate.ConflictTopic, TimeSpan.FromSeconds(30), ctx.CancellationToken, x => x.Operation.EntityIdentifier == remote.Identifier);
            Assert.Contains("server copy", conflict.RemotePayload);

            var received = await ctx.Events.WaitAsync<SyncReceivedItem>(TestDataSyncDelegate.ReceivedTopic, TimeSpan.FromSeconds(10), ctx.CancellationToken, x => x.Entity is SyncNote n && n.Identifier == remote.Identifier);
            Assert.Equal(SyncVerb.Update, received.Verb);
            Assert.Equal(remote, received.Entity);

            await Task.Delay(2000, ctx.CancellationToken);
            Assert.Single(await GetWrites(ctx, remote.Identifier));
            Assert.Equal(remote, await GetServerNote(ctx, remote.Identifier));
            await this.AssertNotPending(remote.Identifier);
        });


        this.Test("Conflict_KeepLocal_ResendsLocalCopy", async ctx =>
        {
            var remote = NewNote("server copy");
            await PutServerNote(ctx, remote);
            await Post(ctx, $"sync/test/conflict/{remote.Identifier}?count=1");
            state.ConflictResolver = (_, _) => ConflictResolution.KeepLocal;

            var local = remote with { Title = "device wins", Revision = 2 };
            await manager.Queue(SyncVerb.Update, local);
            await ctx.Events.WaitAsync<SyncConflict>(TestDataSyncDelegate.ConflictTopic, TimeSpan.FromSeconds(30), ctx.CancellationToken, x => x.Operation.EntityIdentifier == remote.Identifier);

            await this.WaitForServerNote(ctx, local, TimeSpan.FromSeconds(40), "the re-queued local copy to be sent after KeepLocal");
            Assert.Equal(2, (await GetWrites(ctx, remote.Identifier)).Count);
        }, TimeSpan.FromSeconds(75));


        this.Test("Conflict_UseMerged_SendsMergedPayload", async ctx =>
        {
            var remote = NewNote("server copy");
            await PutServerNote(ctx, remote);
            await Post(ctx, $"sync/test/conflict/{remote.Identifier}?count=1");

            var merged = remote with { Title = "merged copy", Revision = 3 };
            state.ConflictResolver = (_, _) => ConflictResolution.UseMerged(Json.Default.Serialize(merged));

            await manager.Queue(SyncVerb.Update, remote with { Title = "device copy" });
            await this.WaitForServerNote(ctx, merged, TimeSpan.FromSeconds(40), "the merged payload to be sent after UseMerged");
        }, TimeSpan.FromSeconds(75));
    }


    async Task<SyncSent> WaitForSent(TestContext ctx, string entityId, TimeSpan? timeout = null)
        => await ctx.Events.WaitAsync<SyncSent>(
            TestDataSyncDelegate.SentTopic,
            timeout ?? TimeSpan.FromSeconds(30),
            ctx.CancellationToken,
            x => x.Operation.EntityIdentifier == entityId
        );


    async Task<SyncPullCompletion> PullAndWait(TestContext ctx)
    {
        var tcs = new TaskCompletionSource<SyncPullCompletion>(TaskCreationOptions.RunContinuationsAsynchronously);
        EventHandler<SyncPullCompletion> handler = (_, c) =>
        {
            if (c.EndpointKey == EndpointKey)
                tcs.TrySetResult(c);
        };

        manager.PullCompleted += handler;
        try
        {
            await manager.PullNow<SyncNote>(ctx.CancellationToken);
            var completion = await tcs.Task.WaitAsync(TimeSpan.FromSeconds(30), ctx.CancellationToken);
            if (completion.Error != null)
                throw new InvalidOperationException("Pull failed", completion.Error);

            return completion;
        }
        finally
        {
            manager.PullCompleted -= handler;
        }
    }


    async Task AssertNotPending(string entityId)
    {
        var pending = await manager.GetPending();
        Assert.DoesNotContain(pending, x => x.EntityIdentifier == entityId);
    }


    async Task WaitForServerNote(TestContext ctx, SyncNote expected, TimeSpan timeout, string because)
    {
        var deadline = DateTime.UtcNow + timeout;
        SyncNote? current = null;
        while (DateTime.UtcNow < deadline)
        {
            current = await GetServerNote(ctx, expected.Identifier);
            if (current == expected)
                return;

            await Task.Delay(500, ctx.CancellationToken);
        }
        var pending = await manager.GetPending();
        var op = pending.FirstOrDefault(x => x.EntityIdentifier == expected.Identifier);
        throw new TimeoutException($"Timed out waiting for {because} - server has '{current?.Title}', op state {op?.State.ToString() ?? "<not queued>"}");
    }


    static List<SyncNote> ReceivedNotes(TestContext ctx) => ctx
        .Events
        .Get<SyncReceivedItem>(TestDataSyncDelegate.ReceivedTopic)
        .Select(x => x.Entity)
        .OfType<SyncNote>()
        .ToList();


    static SyncNote NewNote(string title) => new("note-" + Guid.NewGuid().ToString("N")[..12], title, 1);


    // ── fixture calls ──
    static async Task Post(TestContext ctx, string relative, string? json = null)
    {
        var content = json == null ? null : new StringContent(json, Encoding.UTF8, "application/json");
        var response = await ctx.Host.PostAsync(relative, content, ctx.CancellationToken);
        response.EnsureSuccessStatusCode();
    }

    // a server-side change the client should pull
    static Task Seed(TestContext ctx, SyncNote note, string verb = "Create")
        => Post(ctx, $"sync/test/seed/{note.Identifier}?verb={verb}", Json.Default.Serialize(note));

    // an entity the server holds without announcing it on the feed
    static async Task PutServerNote(TestContext ctx, SyncNote note)
    {
        using var http = new HttpClient();
        var response = await http.PutAsync(
            ctx.Host.Url($"sync/test/entity/{note.Identifier}"),
            new StringContent(Json.Default.Serialize(note), Encoding.UTF8, "application/json"),
            ctx.CancellationToken
        );
        response.EnsureSuccessStatusCode();
    }

    static async Task<SyncNote?> GetServerNote(TestContext ctx, string id)
    {
        var response = await ctx.Host.GetAsync($"sync/test/entity/{id}", ctx.CancellationToken);
        if (response.StatusCode == HttpStatusCode.NotFound)
            return null;

        response.EnsureSuccessStatusCode();
        return Json.Default.Deserialize<SyncNote>(await response.Content.ReadAsStringAsync(ctx.CancellationToken));
    }

    static async Task<List<SyncWrite>> GetWrites(TestContext ctx, string id)
    {
        var all = await ctx.Host.GetAsync("sync/test/writes", ctx.CancellationToken);
        all.EnsureSuccessStatusCode();
        var writes = await all.Content.ReadFromJsonAsync(DataSyncFixtureJsonContext.Default.ListSyncWrite, ctx.CancellationToken) ?? [];
        return writes.Where(x => x.EntityId == id).ToList();
    }

    static async Task<List<string?>> GetPulls(TestContext ctx)
    {
        var response = await ctx.Host.GetAsync("sync/test/pulls", ctx.CancellationToken);
        response.EnsureSuccessStatusCode();
        var pulls = await response.Content.ReadFromJsonAsync(DataSyncFixtureJsonContext.Default.ListString, ctx.CancellationToken) ?? [];
        return pulls.Select(x => (string?)x).ToList();
    }
}
