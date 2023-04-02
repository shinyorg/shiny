using System;
using System.Collections.Generic;
using System.Linq;
using System.Text.Json;
using Shiny.Stores;

namespace Shiny.Support.Repositories.Impl;


public abstract class RepositoryConverter<T> : IRepositoryConverter<T> where T : IRepositoryEntity
{
    public abstract T FromStore(IDictionary<string, object> values, ISerializer serializer);


    public virtual IEnumerable<(string Property, object Value)> ToStore(T entity, ISerializer serializer)
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
    protected TValue? ConvertFromStoreValue<TValue>(IDictionary<string, object> values, string key, TValue? defaultValue = default)
    {
        if (!values.ContainsKey(key))
            return defaultValue;

        return (TValue)this.ConvertFromStore(typeof(TValue), values[key]);
    }


    //public static bool IsSimple(this Type type) =>
    //    TypeDescriptor.GetConverter(type).CanConvertFrom(typeof(string));
    protected virtual object ConvertFromStore(Type expectedType, object value)
    {
        if (expectedType.IsValueType && Nullable.GetUnderlyingType(expectedType) != null)
            expectedType = Nullable.GetUnderlyingType(expectedType)!;

        // TODO: validate value can be converted to expectedType
        if (expectedType == typeof(TimeSpan))
            return TimeSpan.FromMilliseconds(Convert.ToDouble(value));

        if (expectedType == typeof(Distance))
            return Distance.FromKilometers(Convert.ToDouble(value));

        if (expectedType == typeof(DateTimeOffset))
            return DateTimeOffset.FromUnixTimeSeconds(Convert.ToInt64(value));

        if (expectedType == typeof(IDictionary<string, string>))
            return JsonSerializer.Deserialize<Dictionary<string, string>>((string)value)!;

        if (expectedType.IsEnum)
            return Convert.ToInt32(value);

        return Convert.ChangeType(value, expectedType);
    }
}
