using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Threading;

namespace Shiny.Collections;


public class BindingList<T> : ObservableCollection<T>, INotifyCollectionChanged<T>
{
    readonly ReaderWriterLockSlim locker = new();

    protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    {
        this.locker.EnterWriteLock();
        try
        {
            base.OnCollectionChanged(e);
        }
        finally
        {
            this.locker.ExitWriteLock();
        }
    }
    

    protected override void InsertItem(int index, T item)
    {
        this.locker.EnterWriteLock();
        try
        {
            base.InsertItem(index, item);
        }
        finally
        {
            this.locker.ExitWriteLock();
        }
    }
    

    protected override void RemoveItem(int index)
    {
        this.locker.EnterWriteLock();
        try
        {
            base.RemoveItem(index);
        }
        finally
        {
            this.locker.ExitWriteLock();
        }
    }
    

    protected override void SetItem(int index, T item)
    {
        this.locker.EnterWriteLock();
        try
        {
            base.SetItem(index, item);
        }
        finally
        {
            this.locker.ExitWriteLock();
        }
    }

    
    protected override void ClearItems()
    {
        this.locker.EnterWriteLock();
        try
        {
            base.ClearItems();
        }
        finally
        {
            this.locker.ExitWriteLock();
        }
    }
    

    public void AddRange(params IEnumerable<T> items)
    {
        this.locker.EnterWriteLock();
        try
        {
            foreach (var item in items)
            {
                base.InsertItem(this.Count, item);
            }
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Add, new List<T>(items)));
        }
        finally
        {
            this.locker.ExitWriteLock();
        }
    }

    
    public void RemoveRange(params IEnumerable<T> items)
    {
        this.locker.EnterWriteLock();
        try
        {
            var removedItems = new List<T>();
            foreach (var item in items)
            {
                if (base.Remove(item))
                {
                    removedItems.Add(item);
                }
            }
            if (removedItems.Count > 0)
            {
                this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems));
            }
        }
        finally
        {
            this.locker.ExitWriteLock();
        }
    }
    
    
    public void ReplaceAll(IEnumerable<T> newItems)
    {
        this.locker.EnterWriteLock();
        try
        {
            base.ClearItems();
            foreach (var item in newItems)
            {
                base.InsertItem(this.Count, item);
            }
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
        }
        finally
        {
            this.locker.ExitWriteLock();
        }
    }
}