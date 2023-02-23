using System.Collections.Generic;

namespace Shiny.Stores;


public interface IStoreConverter<TEntity> where TEntity : IStoreEntity
{
    TEntity FromStore(IDictionary<string, object> values, ISerializer serializer);
    IEnumerable<(string Property, object Value)> ToStore(TEntity entity, ISerializer serializer);
}
