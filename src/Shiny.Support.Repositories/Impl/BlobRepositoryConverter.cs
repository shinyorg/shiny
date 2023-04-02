using System.Collections.Generic;
using Shiny.Stores;

namespace Shiny.Support.Repositories.Impl;


public class BlobRepositoryConverter<T> : RepositoryConverter<BlobRepositoryEntity<T>>
{
    public override BlobRepositoryEntity<T> FromStore(IDictionary<string, object> values, ISerializer serializer)
    {
        var id = (string)values["Identifier"];
        var obj = serializer.Deserialize<T>((string)values["Object"]);
        return new BlobRepositoryEntity<T>(id, obj);
    }

    public override IEnumerable<(string Property, object Value)> ToStore(BlobRepositoryEntity<T> entity, ISerializer serializer)
    {
        var obj = serializer.Serialize(entity);
        yield return ("Object", obj);
    }
}


public record BlobRepositoryEntity<T>(
    string Identifier,
    T Object
) : IRepositoryEntity;
