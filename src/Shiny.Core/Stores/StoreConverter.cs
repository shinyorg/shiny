using System.Collections.Generic;

namespace Shiny.Stores;


public class StoreConverter<T> : IStoreConverter<T> where T : IStoreEntity
{
    public virtual T FromStore(IDictionary<string, object> values) => throw new System.NotImplementedException();
    public virtual IEnumerable<(string Property, object value)> ToStore(T entity) => throw new System.NotImplementedException();
}
