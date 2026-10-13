using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Shiny.BluetoothLE.Intrastructure;
using Xunit;

namespace Shiny.BluetoothLE.Tests;


public class WriteFlowControlTests
{
    static readonly TimeSpan Recheck = TimeSpan.FromMilliseconds(250);
    static readonly TimeSpan Timeout = TimeSpan.FromSeconds(10);

    readonly HistoricalScheduler scheduler = new();
    readonly Subject<Unit> readySignal = new();
    bool canSend;
    int readyCount;
    Exception? error;


    IDisposable Wait(TimeSpan? timeout = null) => WriteFlowControl
        .WhenReady(this.readySignal, () => this.canSend, Recheck, timeout ?? Timeout, this.scheduler)
        .Subscribe(_ => this.readyCount++, ex => this.error = ex);


    [Fact]
    public void Completes_WhenReadySignalFires()
    {
        using var sub = this.Wait();
        Assert.Equal(0, this.readyCount);

        this.readySignal.OnNext(Unit.Default);

        Assert.Equal(1, this.readyCount);
        Assert.Null(this.error);
    }


    [Fact]
    public void Completes_Immediately_WhenFlagFlippedBeforeSubscribe()
    {
        // the #1657 race: the ready callback landed between the caller's check and the wait
        this.canSend = true;

        using var sub = this.Wait();

        Assert.Equal(1, this.readyCount);
    }


    [Fact]
    public void Completes_WhenFlagFlipsWithoutReadySignal()
    {
        using var sub = this.Wait();

        this.canSend = true;
        Assert.Equal(0, this.readyCount);
        this.scheduler.AdvanceBy(Recheck);

        Assert.Equal(1, this.readyCount);
        Assert.Null(this.error);
    }


    [Fact]
    public void Fails_WithBleException_WhenNeverReady()
    {
        using var sub = this.Wait();

        this.scheduler.AdvanceBy(Timeout - TimeSpan.FromTicks(1));
        Assert.Null(this.error);
        this.scheduler.AdvanceBy(TimeSpan.FromTicks(1));

        Assert.IsType<BleException>(this.error);
        Assert.Equal(0, this.readyCount);
    }


    [Fact]
    public void InfiniteTimeout_NeverFails_ButStillRechecks()
    {
        using var sub = this.Wait(System.Threading.Timeout.InfiniteTimeSpan);

        this.scheduler.AdvanceBy(TimeSpan.FromHours(1));
        Assert.Null(this.error);
        Assert.Equal(0, this.readyCount);

        this.canSend = true;
        this.scheduler.AdvanceBy(Recheck);
        Assert.Equal(1, this.readyCount);
    }


    [Fact]
    public void YieldsOnce_WhenSignalAndRecheckBothObserveReady()
    {
        using var sub = this.Wait();

        this.canSend = true;
        this.readySignal.OnNext(Unit.Default);
        this.scheduler.AdvanceBy(Timeout);

        Assert.Equal(1, this.readyCount);
        Assert.Null(this.error);
    }


    [Fact]
    public void StopsPolling_OnceReady()
    {
        var reads = 0;
        using var sub = WriteFlowControl
            .WhenReady(this.readySignal, () => { reads++; return false; }, Recheck, Timeout, this.scheduler)
            .Subscribe(_ => { }, _ => { });

        this.readySignal.OnNext(Unit.Default);
        var readsWhenReady = reads;
        this.scheduler.AdvanceBy(Timeout);

        Assert.Equal(readsWhenReady, reads);
    }
}
