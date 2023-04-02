using System.Collections.Generic;
using Shiny.Stores;

namespace Shiny.Support.Repositories;


public interface IRepositoryConverter<TEntity> where TEntity : IRepositoryEntity
{
    TEntity FromStore(IDictionary<string, object> values, ISerializer serializer);
    IEnumerable<(string Property, object Value)> ToStore(TEntity entity, ISerializer serializer);
}
