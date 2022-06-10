using System;
using System.Collections.Generic;
using System.Linq;

namespace Shiny.Stores;


//TODO: I want to also include logic for reading/writing to the props dictionary - could use expressions
public abstract class StoreConverter<T> : IStoreConverter<T> where T : IStoreEntity
{
    public abstract T FromStore(IDictionary<string, object> values);


    public virtual IEnumerable<(string Property, object Value)> ToStore(T entity)
    {
        var props = typeof(T).GetProperties().Where(x => x.GetMethod != null).ToList();
        foreach (var prop in props)
        {
            var value = prop.GetValue(entity);
            if (value != null)
            { 
                var converted = this.ConvertToStoreValue(value);
                yield return (prop.Name, converted);
            }
        }
    }


    protected virtual object ConvertToStoreValue(object value)
    {
        if (value is TimeSpan ts) 
            return ts.TotalMilliseconds;

        if (value is Distance dist)
            return dist.TotalKilometers;

        if (value is DateTimeOffset ds)
            return ds.ToUnixTimeSeconds();

        // TODO: ensure simple type
        return value;
    }


    
    protected virtual object ConvertFromStore(Type expectedType, object value)
    {
        // TODO: validate value can be converted to expectedType
        if (expectedType == typeof(TimeSpan))
            return TimeSpan.FromMilliseconds((double)value); 

        if (expectedType == typeof(Distance))
            return Distance.FromKilometers((double)value);

        if (expectedType == typeof(DateTimeOffset))
            return DateTimeOffset.FromUnixTimeSeconds((long)value);

        return Convert.ChangeType(value, expectedType);
    }
}
