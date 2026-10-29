using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging.Abstractions;
using Shiny.Nfc.Infrastructure;
using Xunit;

namespace Shiny.Nfc.Tests;


public class ManagerTests
{
    [Fact]
    public async Task Scan_StartsOnFirstMoveNext_AndStopsWhenTheLoopExits()
    {
        var manager = new FakeNfcManager();
        var tag = new FakeTag();

        var enumerator = manager.Scan().GetAsyncEnumerator();
        Assert.Equal(0, manager.StartCount);

        var move = enumerator.MoveNextAsync();
        var sink = await manager.Started;
        Assert.Equal(1, manager.StartCount);

        sink.Report(tag);
        Assert.True(await move);
        Assert.Same(tag, enumerator.Current);
        Assert.False(manager.SessionDisposed);

        await enumerator.DisposeAsync();
        Assert.True(manager.SessionDisposed);
    }


    [Fact]
    public async Task SecondConcurrentScan_Throws_AndAScanAfterwardsWorks()
    {
        var manager = new FakeNfcManager();
        using var cts = new CancellationTokenSource();
        var first = manager.Scan(ct: cts.Token).GetAsyncEnumerator();
        var firstMove = first.MoveNextAsync();
        await manager.Started;

        await Assert.ThrowsAsync<InvalidOperationException>(async () =>
        {
            await foreach (var _ in manager.Scan()) { }
        });

        cts.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await firstMove);
        await first.DisposeAsync();

        manager.Reset();
        var second = manager.Scan().GetAsyncEnumerator();
        var move = second.MoveNextAsync();
        (await manager.Started).Report(new FakeTag());
        Assert.True(await move);
        await second.DisposeAsync();
    }


    [Fact]
    public async Task PlatformFailure_IsRethrownFromTheLoop()
    {
        var manager = new FakeNfcManager();
        var enumerator = manager.Scan().GetAsyncEnumerator();
        var move = enumerator.MoveNextAsync();

        (await manager.Started).Fail(new NfcException("radio died"));

        var ex = await Assert.ThrowsAsync<NfcException>(async () => await move);
        Assert.Equal("radio died", ex.Message);
        await enumerator.DisposeAsync();
        Assert.True(manager.SessionDisposed);
    }


    [Fact]
    public async Task UserDismissal_EndsTheLoopNormally()
    {
        var manager = new FakeNfcManager();
        var read = manager.ReadOne();

        (await manager.Started).Complete();

        Assert.Null(await read);
        Assert.True(manager.SessionDisposed);
    }


    [Fact]
    public async Task BusyLoop_KeepsOnlyTheNewestTag_AndDisposesTheDroppedOne()
    {
        var manager = new FakeNfcManager();
        var enumerator = manager.Scan().GetAsyncEnumerator();
        var move = enumerator.MoveNextAsync();
        var sink = await manager.Started;

        var handled = new FakeTag();
        sink.Report(handled);
        Assert.True(await move);

        // two more arrive while the loop body is still busy with the first
        var older = new FakeTag();
        var newer = new FakeTag();
        sink.Report(older);
        sink.Report(newer);

        Assert.True(await enumerator.MoveNextAsync());
        Assert.Same(newer, enumerator.Current);
        Assert.True(older.Disposed);

        await enumerator.DisposeAsync();
    }


    [Fact]
    public async Task Cancellation_StopsTheSession()
    {
        var manager = new FakeNfcManager();
        using var cts = new CancellationTokenSource();
        var enumerator = manager.Scan(ct: cts.Token).GetAsyncEnumerator();
        var move = enumerator.MoveNextAsync();
        await manager.Started;

        cts.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(async () => await move);
        await enumerator.DisposeAsync();
        Assert.True(manager.SessionDisposed);
    }


    [Fact]
    public async Task ReadOne_ReadsInsideTheSession()
    {
        var manager = new FakeNfcManager();
        var message = new NdefMessage(NdefRecord.CreateText("hi"));
        var tag = new FakeTag { Message = message, Identifier = [1, 2, 3, 4], Owner = manager };

        var read = manager.ReadOne();
        (await manager.Started).Report(tag);
        var result = await read;

        Assert.NotNull(result);
        Assert.Equal(message, result.Message);
        Assert.Equal(new byte[] { 1, 2, 3, 4 }, result.Identifier);
        Assert.False(tag.ReadAfterSessionEnded);
    }


    [Fact]
    public async Task WriteOne_WritesInsideTheSession()
    {
        var manager = new FakeNfcManager();
        var message = new NdefMessage(NdefRecord.CreateUri("https://shinylib.net"));
        var tag = new FakeTag { Owner = manager };

        var write = manager.WriteOne(message);
        (await manager.Started).Report(tag);

        Assert.True(await write);
        Assert.Equal(message, tag.Message);
        Assert.False(tag.ReadAfterSessionEnded);
    }


    [Fact]
    public async Task NotSupported_ReportsNothing_AndScanThrows()
    {
        var manager = new NotSupportedNfcManager(NullLogger<NotSupportedNfcManager>.Instance);

        Assert.Equal(NfcCapabilities.None, manager.Capabilities);
        Assert.Equal(AccessState.NotSupported, await manager.RequestAccess());

        // twice - a failed start must release the one-scan-at-a-time gate
        for (var i = 0; i < 2; i++)
            await Assert.ThrowsAsync<NfcNotSupportedException>(() => manager.ReadOne());
    }


    [Fact]
    public void AddNotSupportedNfc_Resolves()
    {
        var services = new ServiceCollection();
        services.AddLogging();
        services.AddNotSupportedNfc();

        using var sp = services.BuildServiceProvider(new ServiceProviderOptions { ValidateOnBuild = true });
        Assert.NotNull(sp.GetRequiredService<INfcManager>());
    }


    class FakeNfcManager() : AbstractNfcManager(NullLogger.Instance)
    {
        TaskCompletionSource<NfcScanSink> started = new(TaskCreationOptions.RunContinuationsAsynchronously);

        public int StartCount { get; private set; }
        public bool SessionDisposed { get; private set; }
        public Task<NfcScanSink> Started => this.started.Task;

        public void Reset()
        {
            this.started = new(TaskCreationOptions.RunContinuationsAsynchronously);
            this.SessionDisposed = false;
        }

        public override NfcCapabilities Capabilities => NfcCapabilities.ReadNdef | NfcCapabilities.WriteNdef;
        public override Task<AccessState> RequestAccess(CancellationToken ct = default) => Task.FromResult(AccessState.Available);

        protected override Task<IAsyncDisposable> StartScan(NfcScanOptions options, NfcScanSink sink, CancellationToken ct)
        {
            this.StartCount++;
            this.started.TrySetResult(sink);
            return Task.FromResult<IAsyncDisposable>(new Session(this));
        }

        class Session(FakeNfcManager manager) : IAsyncDisposable
        {
            public ValueTask DisposeAsync()
            {
                manager.SessionDisposed = true;
                return ValueTask.CompletedTask;
            }
        }
    }


    class FakeTag : INfcTag, IDisposable
    {
        public FakeNfcManager? Owner { get; init; }
        bool SessionOpen => this.Owner is not { SessionDisposed: true };

        public byte[]? Identifier { get; init; }
        public bool IsWritable => true;
        public int? MaxSize => 137;
        public NdefMessage? Message { get; set; }
        public bool Disposed { get; private set; }
        public bool ReadAfterSessionEnded { get; private set; }

        public Task<NdefMessage?> ReadNdef(CancellationToken ct = default)
        {
            this.ReadAfterSessionEnded |= !SessionOpen;
            return Task.FromResult(this.Message);
        }

        public Task WriteNdef(NdefMessage message, CancellationToken ct = default)
        {
            this.ReadAfterSessionEnded |= !SessionOpen;
            this.Message = message;
            return Task.CompletedTask;
        }

        public Task MakeReadOnly(CancellationToken ct = default) => Task.CompletedTask;
        public void Dispose() => this.Disposed = true;
    }
}
