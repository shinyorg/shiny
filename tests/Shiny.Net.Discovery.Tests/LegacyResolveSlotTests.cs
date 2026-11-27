using Shiny.Net.Discovery.Internals;
using Xunit;

namespace Shiny.Net.Discovery.Tests;


/// <summary>
/// The pre-API 34 NsdManager resolve slot: a timed-out resolve keeps the slot until its listener
/// reports back, live resolves queue callers, orphaned ones send them elsewhere.
/// </summary>
public class LegacyResolveSlotTests
{
    static readonly TimeSpan Short = TimeSpan.FromMilliseconds(100);
    static readonly TimeSpan Long = TimeSpan.FromSeconds(10);


    [Fact]
    public async Task FreeSlot_IsAcquired()
    {
        var slot = new LegacyResolveSlot();

        var (outcome, ticket) = await slot.AcquireAsync(Short, CancellationToken.None);

        Assert.Equal(LegacyResolveSlotOutcome.Acquired, outcome);
        Assert.NotNull(ticket);
        Assert.True(slot.IsBusy);
    }


    [Fact]
    public async Task LiveHolder_QueuesCallerUntilItCompletes()
    {
        var slot = new LegacyResolveSlot();
        var (_, first) = await slot.AcquireAsync(Short, CancellationToken.None);

        var waiting = slot.AcquireAsync(Long, CancellationToken.None);
        await Task.Delay(50);
        Assert.False(waiting.IsCompleted);

        first!.Complete();
        var (outcome, second) = await waiting;

        Assert.Equal(LegacyResolveSlotOutcome.Acquired, outcome);
        Assert.NotNull(second);
        Assert.NotSame(first, second);
    }


    [Fact]
    public async Task LiveHolder_CallerTimesOutOnItsOwnWindow()
    {
        var slot = new LegacyResolveSlot();
        await slot.AcquireAsync(Short, CancellationToken.None);

        var (outcome, ticket) = await slot.AcquireAsync(Short, CancellationToken.None);

        Assert.Equal(LegacyResolveSlotOutcome.TimedOut, outcome);
        Assert.Null(ticket);
        Assert.True(slot.IsBusy);
    }


    [Fact]
    public async Task Orphan_DoesNotFreeTheSlot()
    {
        var slot = new LegacyResolveSlot();
        var (_, first) = await slot.AcquireAsync(Short, CancellationToken.None);

        first!.Orphan();

        Assert.True(slot.IsBusy);
        Assert.True(slot.IsOrphaned);

        var (outcome, ticket) = await slot.AcquireAsync(Long, CancellationToken.None);
        Assert.Equal(LegacyResolveSlotOutcome.Orphaned, outcome);
        Assert.Null(ticket);
    }


    [Fact]
    public async Task Orphan_WakesQueuedCallers()
    {
        var slot = new LegacyResolveSlot();
        var (_, first) = await slot.AcquireAsync(Short, CancellationToken.None);

        var waiting = slot.AcquireAsync(Long, CancellationToken.None);
        await Task.Delay(50);

        first!.Orphan();
        var (outcome, _) = await waiting.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(LegacyResolveSlotOutcome.Orphaned, outcome);
    }


    [Fact]
    public async Task OrphanReportingBack_FreesTheSlot()
    {
        var slot = new LegacyResolveSlot();
        var (_, first) = await slot.AcquireAsync(Short, CancellationToken.None);
        first!.Orphan();

        first.Complete();

        Assert.False(slot.IsBusy);
        var (outcome, second) = await slot.AcquireAsync(Short, CancellationToken.None);
        Assert.Equal(LegacyResolveSlotOutcome.Acquired, outcome);
        Assert.NotNull(second);
    }


    [Fact]
    public async Task WaitBehindOrphan_AcquiresWhenTheOrphanReportsBack()
    {
        var slot = new LegacyResolveSlot();
        var (_, first) = await slot.AcquireAsync(Short, CancellationToken.None);
        first!.Orphan();

        var waiting = slot.AcquireAsync(Long, CancellationToken.None, waitBehindOrphan: true);
        await Task.Delay(50);
        Assert.False(waiting.IsCompleted);

        first.Complete();
        var (outcome, second) = await waiting.WaitAsync(TimeSpan.FromSeconds(2));

        Assert.Equal(LegacyResolveSlotOutcome.Acquired, outcome);
        Assert.NotNull(second);
    }


    [Fact]
    public async Task WaitBehindOrphan_TimesOutWhenItNeverReportsBack()
    {
        var slot = new LegacyResolveSlot();
        var (_, first) = await slot.AcquireAsync(Short, CancellationToken.None);
        first!.Orphan();

        var (outcome, _) = await slot.AcquireAsync(Short, CancellationToken.None, waitBehindOrphan: true);

        Assert.Equal(LegacyResolveSlotOutcome.TimedOut, outcome);
    }


    [Fact]
    public async Task StaleTicket_CannotTouchTheNextHolder()
    {
        var slot = new LegacyResolveSlot();
        var (_, first) = await slot.AcquireAsync(Short, CancellationToken.None);
        first!.Complete();
        var (_, second) = await slot.AcquireAsync(Short, CancellationToken.None);

        // a late duplicate callback from the first resolve
        first.Complete();
        first.Orphan();

        Assert.True(slot.IsBusy);
        Assert.False(slot.IsOrphaned);
        second!.Complete();
        Assert.False(slot.IsBusy);
    }


    [Fact]
    public async Task CompleteAfterTimeoutRace_LeavesSlotFree()
    {
        var slot = new LegacyResolveSlot();
        var (_, first) = await slot.AcquireAsync(Short, CancellationToken.None);

        // listener reported back just before the caller's timeout fired
        first!.Complete();
        first.Orphan();

        Assert.False(slot.IsBusy);
        Assert.False(slot.IsOrphaned);
    }


    [Fact]
    public async Task Cancellation_Throws()
    {
        var slot = new LegacyResolveSlot();
        await slot.AcquireAsync(Short, CancellationToken.None);
        using var cts = new CancellationTokenSource(50);

        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => slot.AcquireAsync(Long, cts.Token)
        );
    }
}
