using System;
using System.Collections.Generic;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Reflection;

namespace Shiny;


public record ItemChanged<T>(
    T Object,
    string? PropertyName
)
{
    public object? GetValue()
        => this.PropertyName == null ? null : this.Object!.GetValue(this.PropertyName);
}


public static class RxObservableExtensions
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
    /// Adds a disposable to a collection and returns it for fluent chaining.
    /// Works with DisposableCollection and CompositeDisposable (both implement ICollection&lt;IDisposable&gt;).
    /// </summary>
    public static T DisposedBy<T>(this T @this, ICollection<IDisposable> collection) where T : IDisposable
    {
        collection.Add(@this);
        return @this;
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


    /// <summary>
    /// Monitors an INPC object for property changes
    /// </summary>
    public static IObservable<TRet?> WhenAnyProperty<TSender, TRet>(this TSender This, Expression<Func<TSender, TRet>> expression) where TSender : INotifyPropertyChanged
    {
        var p = This.GetPropertyInfo(expression);
        return Observable
            .FromEventPattern<PropertyChangedEventArgs>(This, nameof(INotifyPropertyChanged.PropertyChanged))
            .StartWith(new EventPattern<PropertyChangedEventArgs>(This, new PropertyChangedEventArgs(p.Name)))
            .Where(x => x.EventArgs.PropertyName == p.Name)
            .Select(x =>
            {
                var value = (TRet?)p.GetValue(This);
                return value;
            });
    }


    /// <summary>
    /// Notifies whenever a property changes within an INotifyPropertyChanged
    /// </summary>
    public static IObservable<ItemChanged<TSender>> WhenAnyProperty<TSender>(this TSender This) where TSender : INotifyPropertyChanged
        => Observable
            .FromEventPattern<PropertyChangedEventArgs>(This, nameof(INotifyPropertyChanged.PropertyChanged))
            .Select(x => new ItemChanged<TSender>(This, x.EventArgs.PropertyName));


    /// <summary>
    /// Equivalent of switchMap in RXJS
    /// </summary>
    public static IObservable<U> SelectSwitch<T, U>(this IObservable<T> current, Func<T, IObservable<U>> next)
        => current.Select(x => next(x)).Switch();


    /// <summary>
    /// Quick helper method to execute an async select
    /// </summary>
    public static IObservable<U> SelectAsync<T, U>(this IObservable<T> observable, Func<Task<U>> task)
        => observable.Select(x => Observable.FromAsync(() => task())).Switch();


    /// <summary>
    /// Quick helper method to execute an async select
    /// </summary>
    public static IObservable<U> SelectAsync<T, U>(this IObservable<T> observable, Func<CancellationToken, Task<U>> task)
        => observable.Select(x => Observable.FromAsync(ct => task(ct))).Switch();


    /// <summary>
    /// This is to make chaining easier when a scheduler is null
    /// </summary>
    public static IObservable<T> ObserveOnIf<T>(this IObservable<T> ob, IScheduler? scheduler)
    {
        if (scheduler != null)
            ob.ObserveOn(scheduler);

        return ob;
    }


    /// <summary>
    /// Runs an action only once when the first result is received
    /// </summary>
    public static IObservable<T> DoOnce<T>(this IObservable<T> obs, Action<T> action)
    {
        var count = 1;
        return obs.Do(x =>
        {
            if (count == 0)
            {
                Interlocked.Increment(ref count);
                action(x);
            }
        });
    }


    /// <summary>
    /// Async Subscribe done properly
    /// </summary>
    public static IDisposable SubscribeAsync<T>(this IObservable<T> observable, Func<T, Task> onNextAsync)
        => observable
            .Select(x => Observable.FromAsync(() => onNextAsync(x)))
            .Concat()
            .Subscribe();


    /// <summary>
    /// Async Subscribe done properly
    /// </summary>
    public static IDisposable SubscribeAsync<T>(this IObservable<T> observable,
                                                Func<T, Task> onNextAsync,
                                                Action<Exception> onError)
        => observable
            .Select(x => Observable.FromAsync(() => onNextAsync(x)))
            .Concat()
            .Subscribe(
                _ => { },
                onError
            );


    /// <summary>
    /// Async Subscribe done properly
    /// </summary>
    public static IDisposable SubscribeAsync<T>(this IObservable<T> observable,
                                                Func<T, Task> onNextAsync,
                                                Action<Exception> onError,
                                                Action onComplete)
        => observable
            .Select(x => Observable.FromAsync(() => onNextAsync(x)))
            .Concat()
            .Subscribe(
                _ => { },
                onError,
                onComplete
            );


    /// <summary>
    /// Async subscribe done properly while also ensuring that only one async value runs at a time
    /// </summary>
    public static IDisposable SubscribeAsyncConcurrent<T>(this IObservable<T> observable, Func<T, Task> onNextAsync)
        => observable
            .Select(x => Observable.FromAsync(() => onNextAsync(x)))
            .Merge()
            .Subscribe();


    /// <summary>
    /// Async subscribe done properly while also ensuring that only async value runs at a time
    /// </summary>
    public static IDisposable SubscribeAsyncConcurrent<T>(this IObservable<T> observable, Func<T, Task> onNextAsync, int maxConcurrent)
        => observable
            .Select(x => Observable.FromAsync(() => onNextAsync(x)))
            .Merge(maxConcurrent)
            .Subscribe();
}
