using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.Collections.Generic;
using System.Linq;
using System.Threading;

namespace Shiny.Collections;


public class BindingList<T> : ObservableCollection<T>, INotifyCollectionChanged<T>
{
    readonly ReaderWriterLockSlim locker = new();

    // protected override void OnCollectionChanged(NotifyCollectionChangedEventArgs e)
    // {
    //     this.locker.EnterWriteLock();
    //     try
    //     {
    //         base.OnCollectionChanged(e);
    //     }
    //     finally
    //     {
    //         this.locker.ExitWriteLock();
    //     }
    // }
    

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
        if (!items.Any())
            return;
        
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
        if (!items.Any())
            return;
        
        var removedItems = new List<T>();
        this.locker.EnterWriteLock();
        try
        {
            
            foreach (var item in items)
            {
                if (base.Remove(item))
                {
                    removedItems.Add(item);
                }
            }
        }
        finally
        {
            this.locker.ExitWriteLock();
        }
        if (removedItems.Count > 0)
        {
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Remove, removedItems));
        }
    }
    
    
    public void ReplaceAll(params IEnumerable<T> newItems)
    {
        if (!newItems.Any())
            return;
        
        this.locker.EnterWriteLock();
        try
        {
            base.ClearItems();
            foreach (var item in newItems)
            {
                base.InsertItem(this.Count, item);
            }
        }
        finally
        {
            this.locker.ExitWriteLock();
        }
        this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(NotifyCollectionChangedAction.Reset));
    }
    
    
    public new IEnumerator<T> GetEnumerator()
    {
        this.locker.EnterReadLock();
        try
        {
            return this.ToList().GetEnumerator();
        }
        finally
        {
            this.locker.ExitReadLock();
        }
    }
}