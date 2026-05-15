using System;
using System.Collections.Generic;
using System.Linq;

namespace Shiny;


internal class ShinySubject<T> : IObservable<T>
{
    readonly HashSet<IObserver<T>> observers = new();


    public void OnCompleted() => this.Pub(x => x.OnCompleted());
    public void OnError(Exception error) => this.Pub(x => x.OnError(error));
    public void OnNext(T value) => this.Pub(x => x.OnNext(value));


    void Pub(Action<IObserver<T>> action)
    {
        List<IObserver<T>> copy;
        lock (this.observers)
            copy = this.observers.ToList();

        foreach (var observer in copy)
        {
            try { action(observer); }
            catch { }
        }
    }


    public IDisposable Subscribe(IObserver<T> observer)
    {
        lock (this.observers)
            this.observers.Add(observer);

        return new ActionDisposable(() =>
        {
            lock (this.observers)
                this.observers.Remove(observer);
        });
    }
}
