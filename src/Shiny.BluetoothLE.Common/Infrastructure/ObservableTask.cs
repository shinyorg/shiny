using System;
using System.ComponentModel;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE.Infrastructure;


/// <summary>
/// Rx bridges for async work that keep a fault arriving after the subscriber has gone from escaping
/// through <see cref="TaskScheduler.UnobservedTaskException"/>.
/// </summary>
/// <remarks>
/// <see cref="Observable.FromAsync{TResult}(Func{CancellationToken, Task{TResult}})"/>, async
/// <see cref="Observable.Create{TResult}(Func{IObserver{TResult}, CancellationToken, Task})"/> leave an
/// exception that arrives after unsubscription sitting on the task. BLE operations fault exactly then - a disconnect
/// tears down the consumer's subscription and also fails the in-flight operation - so every mid-operation disconnect
/// reached crash telemetry as an unobserved task exception (#1674). Live subscribers still get OnError as before.
/// </remarks>
[EditorBrowsable(EditorBrowsableState.Never)]
public static class ObservableTask
{
    static readonly TaskObservationOptions Options = new(null, ignoreExceptionsAfterUnsubscribe: true);


    public static IObservable<T> FromAsync<T>(Func<CancellationToken, Task<T>> func)
        => Observable.FromAsync(func, Options);


    public static IObservable<T> FromAsync<T>(Func<Task<T>> func)
        => Observable.FromAsync(func, Options);


    public static IObservable<Unit> FromAsync(Func<CancellationToken, Task> func)
        => Observable.FromAsync(func, Options);


    public static IObservable<T> Create<T>(Func<IObserver<T>, CancellationToken, Task> subscribeAsync) => Observable.Create<T>(async (ob, ct) =>
    {
        try
        {
            await subscribeAsync(ob, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // Rx forwards the task's fault to OnError only while subscribed - after Dispose nothing observes
            // the task, so the fault surfaced as an unobserved task exception. Reporting it here does the same
            // thing for a live subscriber, is a no-op on a stopped observer, and the task never faults
            ob.OnError(ex);
        }
    });
}
