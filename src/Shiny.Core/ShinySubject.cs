using System;
using System.Collections.Generic;
using System.Reactive.Disposables;
using System.Reactive.Subjects;
using Microsoft.Extensions.Logging;

namespace Shiny;


public class ShinySubject<T> : ISubject<T>
{
    readonly HashSet<IObserver<T>> observers = new();

    public ShinySubject(ILogger? logger = null) => this.Logger = logger;
    public ILogger? Logger { get; set; }
    

    public void OnCompleted()
    {
        foreach (var observer in this.observers)
        {
            try
            {
                observer.OnCompleted();
            }
            catch (Exception ex)
            {
                this.Logger?.LogWarning(ex, "ShinySubject OnCompleted error on observer");
            }
        }
    }


    public void OnError(Exception error)
    {
        foreach (var observer in this.observers)
        {
            try
            {
                observer.OnError(error);
            }
            catch (Exception ex)
            {
                this.Logger?.LogWarning(ex, "ShinySubject OnError error on observer");
            }
        }
    }


    public void OnNext(T value)
    {
        foreach (var observer in this.observers)
        {
            try
            {
                observer.OnNext(value);
            }
            catch (Exception ex)
            {
                this.Logger?.LogWarning(ex, "ShinySubject OnNext error on observer");
            }
        }
    }

    public IDisposable Subscribe(IObserver<T> observer)
    {
        this.observers.Add(observer);
        return Disposable.Create(() => this.observers.Remove(observer));
    }
}
