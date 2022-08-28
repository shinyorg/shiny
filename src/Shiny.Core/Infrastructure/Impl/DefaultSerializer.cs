using System;
using System.Collections.Generic;
using System.Text.Json;

namespace Shiny.Infrastructure.Impl;


public class DefaultSerializer : ISerializer
{
    public T Deserialize<T>(string value) => (T)this.Deserialize(typeof(T), value);
    public object Deserialize(Type objectType, string value)
    {
        var result = JsonSerializer.Deserialize(value, objectType);
        if (objectType == typeof(Dictionary<string, object>))
        {
            var dictionary = result as Dictionary<string, object>;
            if (dictionary == null)
                throw new InvalidCastException("Dictionary did not cast");

            foreach (var pair in dictionary)
            {
                var el = (JsonElement)pair.Value;

                switch (el.ValueKind)
                {
                    case JsonValueKind.String:
                        dictionary[pair.Key] = el.GetString();
                        break;

                    case JsonValueKind.Number:
                        if (el.TryGetInt64(out var longValue))
                        {
                            dictionary[pair.Key] = longValue;
                        }
                        else
                        {
                            dictionary[pair.Key] = el.GetDouble();
                        }
                        break;

                    case JsonValueKind.True:
                    case JsonValueKind.False:
                        dictionary[pair.Key] = el.GetBoolean();
                        break;

                    default:
                        throw new ArgumentException("Invalid ValueKind - " + el.ValueKind);
                }
            }
            result = dictionary;
        }
        return result!;
    }


    public string Serialize(object value) => JsonSerializer.Serialize(value);
}
