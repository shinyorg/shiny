using System;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Concurrency;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny
{
    public class ItemChanged<T, TRet>
    {
        public ItemChanged(T obj, TRet value)
        {
            this.Object = obj;
            this.Value = value;
        }

        public T Object { get; }
        public TRet Value { get; }
    }


    public static partial class Extensions
    {
        /// <summary>
        /// Adds a timeout to a task - make sure to trap the timeout exception
        /// </summary>
        /// <param name="task"></param>
        /// <param name="milliseconds"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public static Task WithTimeout(this Task task, int seconds, CancellationToken cancelToken = default) =>
            task.WithTimeout(TimeSpan.FromSeconds(seconds), cancelToken);


        /// <summary>
        /// Adds a timeout to a task - make sure to trap the timeout exception
        /// </summary>
        /// <param name="task"></param>
        /// <param name="timeSpan"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public static Task WithTimeout(this Task task, TimeSpan timeSpan, CancellationToken cancelToken = default) =>
            Observable
                .FromAsync(() => task)
                .Timeout(timeSpan)
                .ToTask(cancelToken);

        /// <summary>
        /// Passes the last and current values from the stream
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ob"></param>
        /// <returns></returns>
        public static IObservable<Tuple<T, T>> WithPrevious<T>(this IObservable<T> ob)
            => ob.Scan(Tuple.Create(default(T), default(T)), (acc, current) => Tuple.Create(acc.Item2, current));


        /// <summary>
        /// Quick helper method to execute an async select
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="observable"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        public static IObservable<U> SelectAsync<T, U>(this IObservable<T> observable, Func<Task<U>> task)
            => observable.Select(x => Observable.FromAsync(() => task())).Switch();


        /// <summary>
        /// Quick helper method to execute an async select
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="U"></typeparam>
        /// <param name="observable"></param>
        /// <param name="task"></param>
        /// <returns></returns>
        public static IObservable<U> SelectAsync<T, U>(this IObservable<T> observable, Func<CancellationToken, Task<U>> task)
            => observable.Select(x => Observable.FromAsync(ct => task(ct))).Switch();


        /// <summary>
        /// This is to make chaining easier when a scheduler is null
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ob"></param>
        /// <param name="scheduler"></param>
        /// <returns></returns>
        public static IObservable<T> ObserveOnIf<T>(this IObservable<T> ob, IScheduler? scheduler)
        {
            if (scheduler != null)
                ob.ObserveOn(scheduler);

            return ob;
        }


        /// <summary>
        /// Runs an action only once when the first result is received
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="obs"></param>
        /// <param name="action"></param>
        /// <returns></returns>
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
        /// A function from ReactiveUI - useful for non-ui stuff too ;)
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="this"></param>
        /// <param name="compositeDisposable"></param>
        /// <returns></returns>
        public static T DisposedBy<T>(this T @this, CompositeDisposable compositeDisposable) where T : IDisposable
        {
            compositeDisposable.Add(@this);
            return @this;
        }


        /// <summary>
        /// A handy way for replying and completing an observer - common for single valued observables
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ob"></param>
        /// <param name="value"></param>
        public static void Respond<T>(this IObserver<T> ob, T value)
        {
            ob.OnNext(value);
            ob.OnCompleted();
        }


        public static IObservable<TRet> WhenAnyProperty<TSender, TRet>(this TSender This, Expression<Func<TSender, TRet>> expression) where TSender : INotifyPropertyChanged
        {
            var p = This.GetPropertyInfo(expression);
            return Observable
                .FromEventPattern<PropertyChangedEventArgs>(This, nameof(INotifyPropertyChanged.PropertyChanged))
                .StartWith(new EventPattern<PropertyChangedEventArgs>(This, new PropertyChangedEventArgs(p.Name)))
                .Where(x => x.EventArgs.PropertyName == p.Name)
                .Select(x =>
                {
                    var value = (TRet)p.GetValue(This);
                    return value;
                });
        }


        public static IObservable<ItemChanged<TSender, string>> WhenAnyProperty<TSender>(this TSender This) where TSender : INotifyPropertyChanged
            => Observable
                .FromEventPattern<PropertyChangedEventArgs>(This, nameof(INotifyPropertyChanged.PropertyChanged))
                .Select(x => new ItemChanged<TSender, string>(This, x.EventArgs.PropertyName));



        public static IDisposable SubscribeAsync<T>(this IObservable<T> observable, Func<T, Task> onNextAsync)
            => observable
                .Select(x => Observable.FromAsync(() => onNextAsync(x)))
                .Concat()
                .Subscribe();


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

        public static IDisposable SubscribeAsyncConcurrent<T>(this IObservable<T> observable, Func<T, Task> onNextAsync)
            => observable
                .Select(x => Observable.FromAsync(() => onNextAsync(x)))
                .Merge()
                .Subscribe();


        public static IDisposable SubscribeAsyncConcurrent<T>(this IObservable<T> observable, Func<T, Task> onNextAsync, int maxConcurrent)
            => observable
                .Select(x => Observable.FromAsync(() => onNextAsync(x)))
                .Merge(maxConcurrent)
                .Subscribe();
    }
}
