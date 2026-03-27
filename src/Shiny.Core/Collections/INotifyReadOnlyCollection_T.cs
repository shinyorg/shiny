using System.Collections.Generic;
using System.Collections.Specialized;

namespace Shiny;

/// <summary>
/// Represents a read-only collection that provides change notifications
/// </summary>
/// <typeparam name="T">The type of elements in the collection</typeparam>
public interface INotifyReadOnlyCollection<T> : INotifyCollectionChanged, IReadOnlyList<T>
{
}


