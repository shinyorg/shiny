using System;

namespace Shiny.Stores;


/// <summary>
/// Provides serialization and deserialization of objects to and from strings
/// </summary>
public interface ISerializer
{
    /// <summary>
    /// Deserializes a string to the specified type
    /// </summary>
    /// <typeparam name="T">The target type</typeparam>
    /// <param name="value">The serialized string</param>
    /// <returns>The deserialized object</returns>
    T Deserialize<T>(string value);

    /// <summary>
    /// Deserializes a string to the specified type
    /// </summary>
    /// <param name="objectType">The target type</param>
    /// <param name="value">The serialized string</param>
    /// <returns>The deserialized object</returns>
    object Deserialize(Type objectType, string value);

    /// <summary>
    /// Serializes an object to a string representation
    /// </summary>
    /// <param name="value">The object to serialize</param>
    /// <returns>The serialized string</returns>
    string Serialize(object value);
}
