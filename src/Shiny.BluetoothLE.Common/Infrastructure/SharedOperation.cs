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
/// Built for GATT service discovery. Concurrent callers used to each start their own discovery on the same GATT
/// client; here they join the one in flight. The operation lives only as long as somebody is waiting on it: when the
/// last caller gives up (its timeout or cancellation), the operation is cancelled too, so nothing keeps running that
/// nobody asked for. A caller with no timeout of its own is still bounded by the operation's timeout.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public sealed class SharedOperation<T>
{
    readonly object syncLock = new();
    Run? current;


    /// <summary>
    /// Joins the operation in flight, or starts one when none is running (a completed, faulted or abandoned one is
    /// not reused).
    /// </summary>
    /// <param name="operation">The work. Receives a token cancelled by <paramref name="lifetime"/>, <paramref name="timeout"/> or the last caller leaving.</param>
    /// <param name="timeout">The longest the operation may run, whoever is waiting.</param>
    /// <param name="lifetime">Ends the operation early (e.g. the peripheral disconnected).</param>
    /// <param name="cancellationToken">Stops this caller waiting; the operation is cancelled once no caller is left.</param>
    public async Task<T> RunAsync(
        Func<CancellationToken, Task<T>> operation,
        TimeSpan timeout,
        CancellationToken lifetime,
        CancellationToken cancellationToken)
    {
        ArgumentNullException.ThrowIfNull(operation);

        Run run;
        lock (this.syncLock)
        {
            if (this.current == null || this.current.Task.IsCompleted)
                this.current = Run.Start(operation, timeout, lifetime);

            run = this.current;
            run.Waiters++;
        }

        try
        {
            return await run.Task.WaitAsync(cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            var abandon = false;
            lock (this.syncLock)
            {
                run.Waiters--;
                if (run.Waiters == 0 && !run.Task.IsCompleted)
                {
                    abandon = true;
                    // a caller arriving after this point must start fresh, not join a run being cancelled
                    if (this.current == run)
                        this.current = null;
                }
            }
            // outside the lock - cancelling runs the operation's registrations inline
            if (abandon)
                run.Cancel();
        }
    }


    sealed class Run
    {
        readonly CancellationTokenSource cts;
        volatile bool abandoned;

        Run(CancellationTokenSource cts) => this.cts = cts;

        public Task<T> Task { get; private set; } = null!;
        public int Waiters { get; set; }


        public static Run Start(Func<CancellationToken, Task<T>> operation, TimeSpan timeout, CancellationToken lifetime)
        {
            var cts = CancellationTokenSource.CreateLinkedTokenSource(lifetime);
            cts.CancelAfter(timeout);

            var run = new Run(cts);
            run.Task = run.Execute(operation, timeout, lifetime);

            // A run abandoned by its callers, or one that fails after they all left, has nobody to observe it -
            // don't let that surface as an unobserved task exception (see #1674).
            run.Task.ContinueWith(
                static t => _ = t.Exception,
                CancellationToken.None,
                TaskContinuationOptions.OnlyOnFaulted | TaskContinuationOptions.ExecuteSynchronously,
                TaskScheduler.Default
            );
            return run;
        }


        public void Cancel()
        {
            this.abandoned = true;
            try
            {
                this.cts.Cancel();
            }
            catch (ObjectDisposedException)
            {
                // the operation finished between the last waiter leaving and this call - nothing left to stop
            }
        }


        async Task<T> Execute(Func<CancellationToken, Task<T>> operation, TimeSpan timeout, CancellationToken lifetime)
        {
            // yield so the operation never runs inside the caller's lock or on its stack
            await System.Threading.Tasks.Task.Yield();

            try
            {
                return await operation(this.cts.Token).ConfigureAwait(false);
            }
            catch (OperationCanceledException ex) when (!lifetime.IsCancellationRequested && !this.abandoned)
            {
                throw new TimeoutException($"Operation did not complete within {timeout}", ex);
            }
            finally
            {
                this.cts.Dispose();
            }
        }
    }
}
