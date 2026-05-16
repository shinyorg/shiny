namespace Shiny.Stores;


/// <summary>
/// Provides serialization and deserialization of objects to and from strings
/// </summary>
public interface ISerializer
{
    /// <summary>
    /// Deserializes a string to the specified type
    /// </summary>
    T Deserialize<T>(string value);

    /// <summary>
    /// Serializes an object to a string representation
    /// </summary>
    string Serialize<T>(T value);
}
