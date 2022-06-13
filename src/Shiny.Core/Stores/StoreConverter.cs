using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;

namespace Shiny.Stores;


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

        if (value is IDictionary<string, string> values)
            return JsonSerializer.Serialize(values);

        // TODO: ensure simple type
        return value;
    }


    //protected void AddIf(IDictionary<string, object> values, Expression)
    protected TValue? Get<TValue>(IDictionary<string, object> values, string key, TValue defaultValue = default)
    {
        if (!values.ContainsKey(key))
            return defaultValue;

        return (TValue)this.ConvertFromStore(typeof(TValue), values[key]);
    }


    //public static bool IsSimple(this Type type) =>
    //    TypeDescriptor.GetConverter(type).CanConvertFrom(typeof(string));
    protected virtual object ConvertFromStore(Type expectedType, object value)
    {
        // TODO: validate value can be converted to expectedType
        if (expectedType == typeof(TimeSpan))
            return TimeSpan.FromMilliseconds((double)value);

        if (expectedType == typeof(Distance))
            return Distance.FromKilometers((double)value);

        if (expectedType == typeof(DateTimeOffset))
            return DateTimeOffset.FromUnixTimeSeconds((long)value);

        if (expectedType == typeof(IDictionary<string, string>))
            return JsonSerializer.Deserialize<Dictionary<string, string>>((string)value)!;

        return Convert.ChangeType(value, expectedType);
    }
}
