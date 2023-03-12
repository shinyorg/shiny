using System.Collections.Generic;

namespace Shiny.Stores.Impl;


public class BlobStoreConverter<T> : StoreConverter<BlobStore<T>>
{
    public override BlobStore<T> FromStore(IDictionary<string, object> values, ISerializer serializer)
    {
        var id = (string)values["Identifier"];
        var obj = serializer.Deserialize<T>((string)values["Object"]);
        return new BlobStore<T>(id, obj);
    }

    public override IEnumerable<(string Property, object Value)> ToStore(BlobStore<T> entity, ISerializer serializer)
    {
        var obj = serializer.Serialize(entity);
        yield return ("Object", obj);
    }
}


public record BlobStore<T>(
    string Identifier,
    T Object
) : IStoreEntity;
