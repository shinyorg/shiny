using System;
using System.Collections.Concurrent;
using System.Reactive.Linq;
using System.Reactive.Subjects;

namespace Shiny.Infrastructure.Impl;

public class MessageBus : IMessageBus
{
    readonly ConcurrentDictionary<Type, Subject<object>> messageSinks = new ConcurrentDictionary<Type, Subject<object>>();


    public bool HasSubscribers<T>() =>
        this.messageSinks.ContainsKey(typeof(T)) &&
        this.messageSinks[typeof(T)].HasObservers;


    public IObservable<T> Listener<T>() => this.messageSinks
        .GetOrAdd(typeof(T), new Subject<object>())
        .Cast<T>();


    public void Publish(object message)
    {
        if (this.messageSinks.TryGetValue(message.GetType(), out var subj))
            subj.OnNext(message);
    }
}
