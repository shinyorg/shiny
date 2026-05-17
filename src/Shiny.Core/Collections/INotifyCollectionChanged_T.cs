using System.Collections.Generic;
using System.Collections.Specialized;

namespace Shiny.Collections;

/// <summary>
/// A mutable list that raises a single change notification for batch add, remove, and replace operations.
/// </summary>
/// <typeparam name="T">The type of elements in the collection.</typeparam>
public interface INotifyCollectionChanged<T> : INotifyCollectionChanged, IList<T>, INotifyReadOnlyCollection<T>
{
    /// <summary>
    /// Adds the supplied items to the end of the collection and raises a single change notification.
    /// </summary>
    /// <param name="items">The items to add.</param>
    void AddRange(params IEnumerable<T> items);

    /// <summary>
    /// Removes the supplied items from the collection and raises a single change notification.
    /// </summary>
    /// <param name="items">The items to remove.</param>
    void RemoveRange(params IEnumerable<T> items);

    /// <summary>
    /// Clears the collection and adds the supplied items in a single change notification.
    /// </summary>
    /// <param name="items">The replacement items.</param>
    void ReplaceAll(params IEnumerable<T> items);
}