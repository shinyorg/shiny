using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;

namespace Shiny;


public class ShinySubject<T> : ISubject<T>
{
    readonly HashSet<IObserver<T>> observers = new();

    public ShinySubject(ILogger? logger = null) => this.Logger = logger;
    public ILogger? Logger { get; set; }
    

    public void OnCompleted() => this.Pub(x => x.OnCompleted(), "OnCompleted error on observer");
    public void OnError(Exception error) => this.Pub(x => x.OnError(error), "OnError error on observer");
    public void OnNext(T value) => this.Pub(x => x.OnNext(value), "OnNext error on observer"); 


    protected virtual void Pub(Action<IObserver<T>> action, string errorDescription)
    {
        List<IObserver<T>> copy;
        lock (this.observers)
            copy = this.observers.ToList();

        foreach (var observer in copy)
        {
            try
            {
                action(observer);
            }
            catch (Exception ex)
            {
                this.Logger?.LogWarning(ex, errorDescription);
            }
        }
    }


    public IDisposable Subscribe(IObserver<T> observer)
    {
        lock (this.observers)
            this.observers.Add(observer);

        return Disposable.Create(() => this.observers.Remove(observer));
    }
}
