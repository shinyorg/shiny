using System;
using System.Collections;
using System.Collections.Generic;
using System.Linq;

namespace Shiny;


public sealed class DisposableCollection : ICollection<IDisposable>, IDisposable
{
    readonly List<IDisposable> disposables = new();


    public void Add(IDisposable disposable)
    {
        lock (this.disposables)
            this.disposables.Add(disposable);
    }


    public void Dispose()
    {
        List<IDisposable> copy;
        lock (this.disposables)
        {
            copy = this.disposables.ToList();
            this.disposables.Clear();
        }
        foreach (var d in copy)
            d.Dispose();
    }


    public int Count { get { lock (this.disposables) return this.disposables.Count; } }
    public bool IsReadOnly => false;
    public bool Contains(IDisposable item) { lock (this.disposables) return this.disposables.Contains(item); }
    public void CopyTo(IDisposable[] array, int arrayIndex) { lock (this.disposables) this.disposables.CopyTo(array, arrayIndex); }
    public bool Remove(IDisposable item) { lock (this.disposables) return this.disposables.Remove(item); }
    public void Clear() { lock (this.disposables) this.disposables.Clear(); }
    public IEnumerator<IDisposable> GetEnumerator() { lock (this.disposables) return this.disposables.ToList().GetEnumerator(); }
    IEnumerator IEnumerable.GetEnumerator() => this.GetEnumerator();
}
