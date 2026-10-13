using System;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;

namespace Shiny.BluetoothLE.Intrastructure;


/// <summary>
/// The wait a write without response parks on while the platform's send buffer is full. It runs while the
/// operation queue lock is held, so it must always end - otherwise one stalled write freezes every other
/// GATT operation until the peripheral disconnects (#1672).
/// </summary>
internal static class WriteFlowControl
{
    /// <summary>
    /// Yields once, as soon as the platform can take another write without response.
    /// </summary>
    /// <param name="readySignal">The platform's "ready to send" callback. Hot, and raised at most once per drain.</param>
    /// <param name="canSend">Reads the platform's current "can send" flag.</param>
    /// <param name="recheckInterval">How often <paramref name="canSend"/> is re-read while waiting.</param>
    /// <param name="timeout">How long to wait before failing with a <see cref="BleException"/>. <see cref="Timeout.InfiniteTimeSpan"/> waits forever.</param>
    /// <param name="scheduler">Scheduler for the re-check and the timeout.</param>
    public static IObservable<Unit> WhenReady(
        IObservable<Unit> readySignal,
        Func<bool> canSend,
        TimeSpan recheckInterval,
        TimeSpan timeout,
        IScheduler scheduler
    )
    {
        // readySignal is subscribed BEFORE the deferred re-check runs, so a callback that lands between the caller's
        // own check and this wait is still seen (#1657). The interval covers the case that leaves: the flag turning
        // true with no callback to announce it, which nothing else would ever observe because every later write is
        // queued behind this one
        var ready = readySignal
            .Merge(Observable.Defer(() => canSend()
                ? Observable.Return(Unit.Default)
                : Observable.Empty<Unit>()
            ))
            .Merge(Observable
                .Interval(recheckInterval, scheduler)
                .Where(_ => canSend())
                .Select(_ => Unit.Default)
            )
            .Take(1);

        if (timeout == Timeout.InfiniteTimeSpan)
            return ready;

        return ready.Timeout(
            timeout,
            Observable.Throw<Unit>(new BleException($"Timed out after {timeout} waiting to send a write without response - the peripheral is not accepting data")),
            scheduler
        );
    }
}
