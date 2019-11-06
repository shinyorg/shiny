using System;
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


        public static IObservable<T> Listener<T>(this IMessageBus msgBus, string name) => msgBus
            .Listener<NamedMessage<T>>()
            .Where(x => x.Name.Equals(name))
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
