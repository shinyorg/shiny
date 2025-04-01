using System.Collections.Generic;
using System.Collections.Specialized;

namespace Shiny;

public interface INotifyReadOnlyCollection<T> : INotifyCollectionChanged, IReadOnlyList<T>
{
}


