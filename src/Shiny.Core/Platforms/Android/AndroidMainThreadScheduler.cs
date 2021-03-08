using System;
using System.Reactive.Concurrency;


namespace Shiny.Platforms.Android
{
    public class AndroidMainThreadScheduler : IScheduler
    {
        public DateTimeOffset Now => DateTimeOffset.Now;

        public IDisposable Schedule<TState>(TState state, Func<IScheduler, TState, IDisposable> action) => throw new NotImplementedException();
        public IDisposable Schedule<TState>(TState state, TimeSpan dueTime, Func<IScheduler, TState, IDisposable> action) => throw new NotImplementedException();
        public IDisposable Schedule<TState>(TState state, DateTimeOffset dueTime, Func<IScheduler, TState, IDisposable> action) => throw new NotImplementedException();
    }
}
