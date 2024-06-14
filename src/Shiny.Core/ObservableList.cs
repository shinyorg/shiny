using System;
using System.Collections.Generic;
using System.Collections.ObjectModel;
using System.Collections.Specialized;
using System.ComponentModel;

namespace Shiny;


public interface INotifyReadOnlyCollection<T> : INotifyCollectionChanged, IReadOnlyList<T>
{
}


public interface INotifyCollectionChanged<T> : INotifyCollectionChanged, IList<T>, INotifyReadOnlyCollection<T>
{
    void AddRange(IEnumerable<T> items, NotifyCollectionChangedAction notificationMode = NotifyCollectionChangedAction.Add);
    void RemoveRange(IEnumerable<T> items, NotifyCollectionChangedAction notificationMode = NotifyCollectionChangedAction.Reset);
    void ReplaceRange(IEnumerable<T> items);
}


// Author: James Montemagno
// https://github.com/jamesmontemagno/mvvm-helpers/blob/master/MvvmHelpers/ObservableRangeCollection.cs
public class ObservableList<T> : ObservableCollection<T>, INotifyCollectionChanged<T>
{
    /// <summary>
    /// Initializes a new instance of the System.Collections.ObjectModel.ObservableCollection(Of T) class.
    /// </summary>
    public ObservableList() : base()
    {
    }

    /// <summary>
    /// Initializes a new instance of the System.Collections.ObjectModel.ObservableCollection(Of T) class that contains elements copied from the specified collection.
    /// </summary>
    /// <param name="collection">collection: The collection from which the elements are copied.</param>
    /// <exception cref="System.ArgumentNullException">The collection parameter cannot be null.</exception>
    public ObservableList(IEnumerable<T> collection) : base(collection)
    {
    }


    /// <summary>
    /// Adds the elements of the specified collection to the end of the ObservableCollection(Of T).
    /// </summary>
    public void AddRange(IEnumerable<T> collection, NotifyCollectionChangedAction notificationMode = NotifyCollectionChangedAction.Add)
    {
        if (notificationMode != NotifyCollectionChangedAction.Add && notificationMode != NotifyCollectionChangedAction.Reset)
            throw new ArgumentException("Mode must be either Add or Reset for AddRange.", nameof(notificationMode));

        if (collection == null)
            throw new ArgumentNullException(nameof(collection));

        this.CheckReentrancy();
        var startIndex = this.Count;
        var itemsAdded = this.AddArrangeCore(collection);

        if (!itemsAdded)
        {
            if (notificationMode == NotifyCollectionChangedAction.Reset)
            {
                this.RaiseChangeNotificationEvents(action: NotifyCollectionChangedAction.Reset);
            }
            else
            {
                var changedItems = collection is List<T> ? (List<T>)collection : new List<T>(collection);
                this.RaiseChangeNotificationEvents(
                    action: NotifyCollectionChangedAction.Add,
                    changedItems: changedItems,
                    startingIndex: startIndex
                );
            }
        }
    }


    /// <summary>
    /// Removes the first occurence of each item in the specified collection from ObservableCollection(Of T). NOTE: with notificationMode = Remove, removed items starting index is not set because items are not guaranteed to be consecutive.
    /// </summary>
    public void RemoveRange(IEnumerable<T> collection, NotifyCollectionChangedAction notificationMode = NotifyCollectionChangedAction.Reset)
    {
        if (notificationMode != NotifyCollectionChangedAction.Remove && notificationMode != NotifyCollectionChangedAction.Reset)
            throw new ArgumentException("Mode must be either Remove or Reset for RemoveRange.", nameof(notificationMode));

        if (collection == null)
            throw new ArgumentNullException(nameof(collection));

        this.CheckReentrancy();

        if (notificationMode == NotifyCollectionChangedAction.Reset)
        {
            var raiseEvents = false;
            foreach (var item in collection)
            {
                this.Items.Remove(item);
                raiseEvents = true;
            }

            if (raiseEvents)
                this.RaiseChangeNotificationEvents(action: NotifyCollectionChangedAction.Reset);
        }
        else
        {
            var changedItems = new List<T>(collection);
            for (var i = 0; i < changedItems.Count; i++)
            {
                if (!this.Items.Remove(changedItems[i]))
                {
                    changedItems.RemoveAt(i); //Can't use a foreach because changedItems is intended to be (carefully) modified
                    i--;
                }
            }

            if (changedItems.Count > 0)
            {
                this.RaiseChangeNotificationEvents(
                    action: NotifyCollectionChangedAction.Remove,
                    changedItems: changedItems
                );
            }
        }
    }


    /// <summary>
    /// Clears the current collection and replaces it with the specified collection.
    /// </summary>
    public void ReplaceRange(IEnumerable<T> collection)
    {
        if (collection == null)
            throw new ArgumentNullException(nameof(collection));

        this.CheckReentrancy();
        var previouslyEmpty = this.Items.Count == 0;
        this.Items.Clear();

        this.AddArrangeCore(collection);
        var currentlyEmpty = this.Items.Count == 0;

        if (previouslyEmpty && currentlyEmpty)
            return;

        this.RaiseChangeNotificationEvents(action: NotifyCollectionChangedAction.Reset);
    }


    bool AddArrangeCore(IEnumerable<T> collection)
    {
        var itemAdded = false;
        foreach (var item in collection)
        {
            this.Items.Add(item);
            itemAdded = true;
        }
        return itemAdded;
    }


    void RaiseChangeNotificationEvents(NotifyCollectionChangedAction action, List<T> changedItems = null, int startingIndex = -1)
    {
        this.OnPropertyChanged(new PropertyChangedEventArgs(nameof(this.Count)));
        this.OnPropertyChanged(new PropertyChangedEventArgs("Item[]"));

        if (changedItems == null)
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action));
        else
            this.OnCollectionChanged(new NotifyCollectionChangedEventArgs(action, changedItems: changedItems, startingIndex: startingIndex));
    }
}