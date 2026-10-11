namespace Shiny;


/// <summary>
/// The kind of device the app is running on, as reported by <see cref="IPlatform.DeviceIdiom"/>.
/// </summary>
public enum DeviceIdiom
{
    Unknown,
    Phone,
    Tablet,
    Desktop,
    TV,
    Watch,
    Car
}
