using System;

namespace Shiny.Infrastructure;


public interface IMessageBus
{
    bool HasSubscribers<T>();
    void Publish(object message);
    IObservable<T> Listener<T>();
}
