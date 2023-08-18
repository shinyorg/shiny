using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;

namespace Shiny;


public interface INotifyReadOnlyCollection<T> : INotifyCollectionChanged, IReadOnlyList<T>
{
}


public interface INotifyCollectionChanged<T> : INotifyCollectionChanged, IList<T>, INotifyReadOnlyCollection<T>
{
    void AddRange(IEnumerable<T> items);
    void RemoveRange(IEnumerable<T> items);
    void ReplaceAll(IEnumerable<T> items);
}


public class ObservableList<T> : ObservableCollection<T>, INotifyCollectionChanged<T>
{
    public ObservableList() { }
    public ObservableList(IEnumerable<T> items) : base(items) { }


    /// <summary>
    /// Adds a collection of items and then fires the CollectionChanged event - more performant than doing individual adds
    /// </summary>
    /// <param name="items"></param>
    public virtual void AddRange(IEnumerable<T> items)
    {
        foreach (var item in items)
            this.Items.Add(item);

        this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, items));
    }


    public void RemoveRange(IEnumerable<T> items)
    {
        foreach (var item in items)
            this.Items.Remove(item);

        this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, items));
    }


    /// <summary>
    /// Clears and sets a new collection
    /// </summary>
    /// <param name="items"></param>
    public virtual void ReplaceAll(IEnumerable<T> items)
    {
        this.Clear();
        foreach (var item in items)
            this.Items.Add(item);

        this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
}

