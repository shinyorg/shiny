using System;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shiny.BluetoothLE.Infrastructure;
using Xunit;

namespace Shiny.BluetoothLE.Common.Tests;


public class SharedOperationTests
{
    static readonly TimeSpan Long = TimeSpan.FromSeconds(30);


    [Fact]
    public async Task ConcurrentCallers_ShareOneRun()
    {
        var op = new SharedOperation<int>();
        var runs = 0;
        var release = new TaskCompletionSource();

        var callers = Enumerable.Range(0, 5)
            .Select(_ => op.RunAsync(async ct =>
            {
                Interlocked.Increment(ref runs);
                await release.Task.WaitAsync(ct);
                return 42;
            }, Long, CancellationToken.None, CancellationToken.None))
            .ToArray();

        await Task.Delay(50);
        release.SetResult();

        Assert.All(await Task.WhenAll(callers), r => Assert.Equal(42, r));
        Assert.Equal(1, runs);
    }


    [Fact]
    public async Task LastCallerGivingUp_CancelsTheOperation()
    {
        var op = new SharedOperation<int>();
        var runs = 0;
        var cancelled = new TaskCompletionSource();

        Func<CancellationToken, Task<int>> work = async ct =>
        {
            Interlocked.Increment(ref runs);
            using var reg = ct.Register(() => cancelled.TrySetResult());
            await Task.Delay(Timeout.Infinite, ct);
            return 7;
        };

        using var shortTimeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => op.RunAsync(work, Long, CancellationToken.None, shortTimeout.Token)
        );
        await cancelled.Task.WaitAsync(TimeSpan.FromSeconds(5));

        // the abandoned run is not reused - the next caller starts a new one
        var second = op.RunAsync(_ => Task.FromResult(8), Long, CancellationToken.None, CancellationToken.None);
        Assert.Equal(8, await second);
        Assert.Equal(1, runs);
    }


    [Fact]
    public async Task OneCallerGivingUp_OthersKeepWaiting()
    {
        var op = new SharedOperation<int>();
        var runs = 0;
        var release = new TaskCompletionSource();
        CancellationToken operationToken = default;

        Func<CancellationToken, Task<int>> work = async ct =>
        {
            Interlocked.Increment(ref runs);
            operationToken = ct;
            await release.Task.WaitAsync(ct);
            return 7;
        };

        var stays = op.RunAsync(work, Long, CancellationToken.None, CancellationToken.None);
        using var shortTimeout = new CancellationTokenSource(TimeSpan.FromMilliseconds(50));
        await Assert.ThrowsAnyAsync<OperationCanceledException>(
            () => op.RunAsync(work, Long, CancellationToken.None, shortTimeout.Token)
        );
        Assert.False(operationToken.IsCancellationRequested);

        release.SetResult();
        Assert.Equal(7, await stays);
        Assert.Equal(1, runs);
    }


    [Fact]
    public async Task Lifetime_CancelsTheOperation()
    {
        var op = new SharedOperation<int>();
        using var lifetime = new CancellationTokenSource();

        var task = op.RunAsync(async ct =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return 0;
        }, Long, lifetime.Token, CancellationToken.None);

        await Task.Delay(30);
        lifetime.Cancel();

        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => task);
    }


    [Fact]
    public async Task OwnTimeout_FaultsWithTimeoutException()
    {
        var op = new SharedOperation<int>();

        var task = op.RunAsync(async ct =>
        {
            await Task.Delay(Timeout.Infinite, ct);
            return 0;
        }, TimeSpan.FromMilliseconds(50), CancellationToken.None, CancellationToken.None);

        await Assert.ThrowsAsync<TimeoutException>(() => task);
    }


    [Fact]
    public async Task AfterFault_NextCallStartsANewRun()
    {
        var op = new SharedOperation<int>();
        var runs = 0;

        Func<CancellationToken, Task<int>> work = _ =>
        {
            var run = Interlocked.Increment(ref runs);
            return run == 1
                ? Task.FromException<int>(new BleException("discovery failed"))
                : Task.FromResult(run);
        };

        await Assert.ThrowsAsync<BleException>(() => op.RunAsync(work, Long, CancellationToken.None, CancellationToken.None));
        Assert.Equal(2, await op.RunAsync(work, Long, CancellationToken.None, CancellationToken.None));
    }


    [Fact]
    public async Task AfterSuccess_NextCallStartsANewRun()
    {
        var op = new SharedOperation<int>();
        var runs = 0;
        Func<CancellationToken, Task<int>> work = _ => Task.FromResult(Interlocked.Increment(ref runs));

        Assert.Equal(1, await op.RunAsync(work, Long, CancellationToken.None, CancellationToken.None));
        Assert.Equal(2, await op.RunAsync(work, Long, CancellationToken.None, CancellationToken.None));
    }


    [Fact]
    public async Task FaultWithNoWaitingCaller_IsNotUnobserved()
    {
        var unobserved = false;
        EventHandler<UnobservedTaskExceptionEventArgs> handler = (_, e) =>
        {
            if (e.Exception.InnerExceptions.Any(x => x.Message == "late fault"))
                unobserved = true;
        };
        TaskScheduler.UnobservedTaskException += handler;
        try
        {
            await RunAndAbandon();

            for (var i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                await Task.Delay(50);
            }
            Assert.False(unobserved);
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= handler;
        }
    }


    static async Task RunAndAbandon()
    {
        var op = new SharedOperation<int>();
        var gate = new TaskCompletionSource();
        using var giveUp = new CancellationTokenSource();

        var caller = op.RunAsync(async _ =>
        {
            await gate.Task;
            throw new BleException("late fault");
        }, Long, CancellationToken.None, giveUp.Token);

        giveUp.Cancel();
        await Assert.ThrowsAnyAsync<OperationCanceledException>(() => caller);

        gate.SetResult();
        await Task.Delay(50);
    }
}
