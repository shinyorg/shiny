using System;
using System.Reactive;
using System.Reactive.Linq;

namespace Shiny
{
    public interface IMessageBus
    {
        bool HasSubscribers<T>();
        void Publish(object message);
        IObservable<T> Listener<T>();
    }


    public static class MessageBusExtensions
    {
        public static void Publish<T>(this IMessageBus msgBus, string name, T arg)
            => msgBus.Publish(new NamedMessage<T>(name, arg));


        public static void Publish(this IMessageBus msgBus, string name)
            => msgBus.Publish(new NamedMessage<Unit>(name, Unit.Default));


        public static IObservable<T> Listener<T>(this IMessageBus msgBus, string eventName) => msgBus
            .Listener<NamedMessage<T>>()
            .Where(x => x.Name.Equals(eventName))
            .Select(x => x.Arg);


        public static IObservable<Unit> Listener(this IMessageBus msgBus, string eventName) => msgBus
            .Listener<NamedMessage<Unit>>()
            .Where(x => x.Name.Equals(eventName))
            .Select(x => x.Arg);
    }


    public class NamedMessage<T>
    {
        public NamedMessage(string name, T arg)
        {
            this.Name = name;
            this.Arg = arg;
        }

        public string Name { get; }
        public T Arg { get; }
    }
}
