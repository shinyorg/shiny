using System;
using System.Reactive.Linq;
using System.Runtime.CompilerServices;
using System.Threading;
using System.Threading.Tasks;
using Shiny.BluetoothLE.Infrastructure;
using Xunit;

namespace Shiny.BluetoothLE.Common.Tests;


public class ObservableTaskTests
{
    // -------- fault after unsubscribe (#1674) --------

    [Fact]
    public Task FromAsync_FaultAfterUnsubscribe_IsNotUnobserved() => AssertNoUnobserved((gate, marker) =>
        ObservableTask.FromAsync<int>(async ct =>
        {
            await gate.ConfigureAwait(false);
            throw new BleException(marker);
        })
    );


    [Fact]
    public Task FromAsync_NoToken_FaultAfterUnsubscribe_IsNotUnobserved() => AssertNoUnobserved((gate, marker) =>
        ObservableTask.FromAsync<int>(async () =>
        {
            await gate.ConfigureAwait(false);
            throw new BleException(marker);
        })
    );


    [Fact]
    public Task Create_FaultAfterUnsubscribe_IsNotUnobserved() => AssertNoUnobserved((gate, marker) =>
        ObservableTask.Create<int>(async (ob, ct) =>
        {
            await gate.ConfigureAwait(false);
            throw new BleException(marker);
        })
    );


    // -------- live subscribers are unchanged --------

    [Fact]
    public async Task FromAsync_LiveSubscriber_GetsError()
    {
        var ex = await Assert.ThrowsAsync<BleException>(async () =>
            await ObservableTask.FromAsync<int>(async ct =>
            {
                await Task.Yield();
                throw new BleException("live");
            })
        );
        Assert.Equal("live", ex.Message);
    }


    [Fact]
    public async Task FromAsync_LiveSubscriber_GetsValue()
    {
        var value = await ObservableTask.FromAsync(async ct =>
        {
            await Task.Yield();
            return 42;
        });
        Assert.Equal(42, value);
    }


    [Fact]
    public async Task Create_LiveSubscriber_GetsValuesThenError()
    {
        var values = 0;
        var ex = await Assert.ThrowsAsync<BleException>(async () =>
            await ObservableTask
                .Create<int>(async (ob, ct) =>
                {
                    ob.OnNext(1);
                    await Task.Yield();
                    throw new BleException("live");
                })
                .Do(_ => values++)
        );
        Assert.Equal("live", ex.Message);
        Assert.Equal(1, values);
    }


    [Fact]
    public async Task Create_LiveSubscriber_Completes()
    {
        var result = await ObservableTask.Create<int>(async (ob, ct) =>
        {
            await Task.Yield();
            ob.OnNext(7);
        });
        Assert.Equal(7, result);
    }


    static async Task AssertNoUnobserved(Func<Task, string, IObservable<int>> create)
    {
        // the event is process wide and tests run in parallel - only count our own exception
        var marker = Guid.NewGuid().ToString();
        var unobserved = 0;
        EventHandler<UnobservedTaskExceptionEventArgs> handler = (_, e) =>
        {
            if (e.Exception.Flatten().InnerExceptions.Any(x => x.Message == marker))
            {
                Interlocked.Increment(ref unobserved);
                e.SetObserved();
            }
        };

        TaskScheduler.UnobservedTaskException += handler;
        try
        {
            var gate = new TaskCompletionSource();
            var errored = new StrongBox<bool>();
            await RunAndDrop(create, gate, marker, errored);

            // fault the work once nobody is listening, then let the faulted task be collected
            gate.SetResult();
            await Task.Delay(100);
            for (var i = 0; i < 3; i++)
            {
                GC.Collect();
                GC.WaitForPendingFinalizers();
                await Task.Delay(20);
            }
            Assert.Equal(0, unobserved);

            // the fault went nowhere - not to the task, and not to the observer that unsubscribed
            Assert.False(errored.Value);
        }
        finally
        {
            TaskScheduler.UnobservedTaskException -= handler;
        }
    }


    // kept out of line so nothing on the test's stack roots the faulted task
    static Task RunAndDrop(Func<Task, string, IObservable<int>> create, TaskCompletionSource gate, string marker, StrongBox<bool> errored)
    {
        create(gate.Task, marker)
            .Subscribe(_ => { }, _ => errored.Value = true)
            .Dispose();

        return Task.CompletedTask;
    }
}
