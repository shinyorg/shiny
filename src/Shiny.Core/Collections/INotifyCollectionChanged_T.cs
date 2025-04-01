using System.Collections.Generic;
using System.Collections.Specialized;

namespace Shiny.Collections;

public interface INotifyCollectionChanged<T> : INotifyCollectionChanged, IList<T>, INotifyReadOnlyCollection<T>
{
    /// <summary>
    /// Add new items
    /// </summary>
    /// <param name="items"></param>
    void AddRange(params IEnumerable<T> items);
    
    /// <summary>
    /// Removes items as sent
    /// </summary>
    /// <param name="items"></param>
    void RemoveRange(params IEnumerable<T> items);
    
    /// <summary>
    /// Clears all items and adds this new item set
    /// </summary>
    /// <param name="items"></param>
    void ReplaceAll(params IEnumerable<T> items);
}