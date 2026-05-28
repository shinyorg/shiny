using System;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE;

static class RxExtensions
{
    /// <summary>
    /// A handy way for replying and completing an observer - common for single valued observables
    /// </summary>
    public static void Respond<T>(this IObserver<T> ob, T value)
    {
        ob.OnNext(value);
        ob.OnCompleted();
    }
    
    /// <summary>
    /// Converts an IObservable to a Task, completing on the first value or error
    /// </summary>
    public static Task<T> ToTask<T>(this IObservable<T> observable, CancellationToken cancellationToken = default)
    {
        var tcs = new TaskCompletionSource<T>();
        IDisposable? sub = null;

        if (cancellationToken.CanBeCanceled)
            cancellationToken.Register(() =>
            {
                sub?.Dispose();
                tcs.TrySetCanceled();
            });

        sub = observable.Subscribe(
            value => tcs.TrySetResult(value),
            error =>
            {
                sub?.Dispose();
                tcs.TrySetException(error);
            },
            () => { }
        );

        return tcs.Task;
    }
}