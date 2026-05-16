using System;
using System.Collections.Generic;

namespace Shiny.BluetoothLE;


public static class DisposableExtensions
{
    /// <summary>
    /// Adds a disposable to a collection and returns it for fluent chaining.
    /// Works with DisposableCollection and CompositeDisposable (both implement ICollection&lt;IDisposable&gt;).
    /// </summary>
    public static T DisposedBy<T>(this T disposable, ICollection<IDisposable> collection) where T : IDisposable
    {
        collection.Add(disposable);
        return disposable;
    }
}
