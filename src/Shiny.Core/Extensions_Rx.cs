using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;
using System.Linq.Expressions;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;


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


    public static class Extensions_Rx
    {
        /// <summary>
        /// A handy way for replying & completing an observer - common for single valued observables
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="ob"></param>
        /// <param name="value"></param>
        public static void Respond<T>(this IObserver<T> ob, T value)
        {
            ob.OnNext(value);
            ob.OnCompleted();
        }


        public static IObservable<NotifyCollectionChangedEventArgs> WhenCollectionChanged(this INotifyCollectionChanged collection) => Observable.Create<NotifyCollectionChangedEventArgs>(ob =>
        {
            var handler = new NotifyCollectionChangedEventHandler((sender, args) => ob.OnNext(args));
            collection.CollectionChanged += handler;
            return () => collection.CollectionChanged -= handler;
        });
  //      return Observable.Defer(() => MakeWebRequest())
  //.RetryWithBackoffStrategy(retryCount: 4, retryOnError: e => e is WebException)
        //[SuppressMessage("Microsoft.Security", "CA2104:DoNotDeclareReadOnlyMutableReferenceTypes")]
        //public static readonly Func<int, TimeSpan> ExponentialBackoff = n => TimeSpan.FromSeconds(Math.Pow(n, 2));
        //// this function was taken from a Microsoft repo somewhere - slightly altered
        //public static IObservable<T> RetryWithBackoffStrategy<T>(
        //    this IObservable<T> source,
        //    int retryCount = 3,
        //    Func<int, TimeSpan> strategy = null,
        //    Func<Exception, bool> retryOnError = null)
        //{
        //    //strategy = strategy ?? ExpontentialBackoff;

        //    if (retryOnError == null)
        //        retryOnError = e => true;

        //    var attempt = 0;

        //    return Observable
        //        .Defer(() =>
        //    {
        //        return ((++attempt == 1) ? source : source.DelaySubscription(strategy(attempt - 1), scheduler))
        //            .Select(item => new Tuple<bool, T, Exception>(true, item, null))
        //            .Catch<Tuple<bool, T, Exception>, Exception>(e => retryOnError(e)
        //                ? Observable.Throw<Tuple<bool, T, Exception>>(e)
        //                : Observable.Return(new Tuple<bool, T, Exception>(false, default(T), e)));
        //    })
        //    .Retry(retryCount)
        //    .SelectMany(t => t.Item1
        //        ? Observable.Return(t.Item2)
        //        : Observable.Throw<T>(t.Item3));
        //}


        /// <summary>
        /// This will buffer observable pings and timestamp them until the predicate check does not pass
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="thisObs"></param>
        /// <param name="predicate"></param>
        /// <returns></returns>
        public static IObservable<List<Timestamped<T>>> BufferWhile<T>(this IObservable<T> thisObs, Func<T, bool> predicate)
            => Observable.Create<List<Timestamped<T>>>(ob =>
            {
                List<Timestamped<T>> list = null;
                return thisObs
                    .Timestamp()
                    .Subscribe(x =>
                    {
                        if (predicate(x.Value))
                        {
                            if (list == null)
                            {
                                list = new List<Timestamped<T>>();
                            }
                            list.Add(x);
                        }
                        else if (list != null)
                        {
                            ob.OnNext(list);
                            list = null;
                        }
                    });
            });


        /// <summary>
        /// Will watch for changes in any observable item in the ObservableCollection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <param name="collection"></param>
        /// <returns></returns>
        public static IObservable<ItemChanged<T, string>> WhenItemChanged<T>(this ObservableCollection<T> collection)
            where T : INotifyPropertyChanged
            => Observable.Create<ItemChanged<T, string>>(ob =>
            {
                var disp = new CompositeDisposable();
                foreach (var item in collection)
                    disp.Add(item.WhenAnyProperty().Subscribe(ob.OnNext));

                return disp;
            });


        /// <summary>
        /// Will watch for a specific property change with any item in the ObservableCollection
        /// </summary>
        /// <typeparam name="T"></typeparam>
        /// <typeparam name="TRet"></typeparam>
        /// <param name="collection"></param>
        /// <param name="expression"></param>
        /// <returns></returns>
        public static IObservable<ItemChanged<T, TRet>> WhenItemValueChanged<T, TRet>(
            this ObservableCollection<T> collection,
            Expression<Func<T, TRet>> expression) where T : INotifyPropertyChanged =>
            Observable.Create<ItemChanged<T, TRet>>(ob =>
            {
                var disp = new CompositeDisposable();
                foreach (var item in collection)
                {
                    disp.Add(item
                        .WhenAnyProperty(expression)
                        .Subscribe(x => ob.OnNext(new ItemChanged<T, TRet>(item, x)))
                    );
                }

                return disp;
            });


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


        //public static IDisposable ApplyMaxLengthConstraint<T>(this T npc, Expression<Func<T, string>> expression, int maxLength) where T : INotifyPropertyChanged
        //{
        //    var property = npc.GetPropertyInfo(expression);

        //    if (property.PropertyType != typeof(string))
        //        throw new ArgumentException($"You can only use maxlength constraints on string based properties - {npc.GetType()}.{property.Name}");

        //    if (!property.CanWrite)
        //        throw new ArgumentException($"You can only apply maxlength constraints to public setter properties - {npc.GetType()}.{property.Name}");

        //    return npc
        //        .WhenAnyProperty(expression)
        //        .Where(x => x != null && x.Length > maxLength)
        //        .Subscribe(x =>
        //        {
        //            var value = x.Substring(0, maxLength);
        //            property.SetValue(npc, value);
        //        });
        //}
    }
}
