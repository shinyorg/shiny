using System;
using System.ComponentModel;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Infrastructure;


/// <summary>
/// Runs at most one instance of an async operation at a time and lets every caller await that instance with
/// its own cancellation token.
/// </summary>
/// <remarks>
/// Built for GATT service discovery. Discovery used to run inside whichever operation first needed a service,
/// bounded by that operation's timeout. When a read with a short timeout gave up, the discovery was cancelled
/// half-way, the peripheral still needed discovery, and the next read refreshed the cache and started again
/// from scratch - a peripheral whose discovery takes about as long as the read timeout never delivered data.
/// Concurrent callers also each started their own discovery on the same GATT client.
/// Here a caller giving up only stops <i>that caller</i> waiting; the operation keeps running (bounded by its
/// own timeout and the lifetime token) and the next caller reuses it.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class SharedOperation<T>
{
    readonly object syncLock = new();
    Task<T>? inflight;


    /// <summary>
    /// Joins the operation in flight, or starts one when none is running (a completed or faulted one is not
    /// reused).
    /// </summary>
    /// <param name="operation">The work. Receives a token cancelled by <paramref name="lifetime"/> or <paramref name="timeout"/>.</param>
    /// <param name="timeout">How long the operation itself may run, independent of any caller.</param>
    /// <param name="lifetime">Ends the operation early (e.g. the peripheral disconnected).</param>
    /// <param name="cancellationToken">Stops this caller waiting; the operation keeps running for the others.</param>
    public Task<T> RunAsync(
        Func<CancellationToken, Task<T>> operation,
        TimeSpan timeout,
        CancellationToken lifetime,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        Task<T> task;
        lock (this.syncLock)
        {
            if (this.inflight == null || this.inflight.IsCompleted)
            {
                this.inflight = Start(operation, timeout, lifetime);

                // Callers may all have stopped waiting before it fails - don't let that surface as an
                // unobserved task exception (see #1674).
                this.inflight.ContinueWith(
                    static t => _ = t.Exception,
                    CancellationToken.None,
                    TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                    TaskScheduler.Default
                );
            }
            task = this.inflight;
        }
        return task.WaitAsync(cancellationToken);
    }


    static async Task<T> Start(Func<CancellationToken, Task<T>> operation, TimeSpan timeout, CancellationToken lifetime)
    {
        // yield so the operation never runs inside the caller's lock or on its stack
        await Task.Yield();

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(lifetime);
        cts.CancelAfter(timeout);
        try
        {
            return await operation(cts.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException ex) when (!lifetime.IsCancellationRequested && cts.IsCancellationRequested)
        {
            throw new TimeoutException($"Operation did not complete within {timeout}", ex);
        }
    }
}
