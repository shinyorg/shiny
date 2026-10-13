namespace Shiny;


/// <summary>
/// The kind of device the app is running on, as reported by <see cref="IPlatform.DeviceFormFactor"/>.
/// </summary>
public enum DeviceFormFactor
{
    Unknown,
    Phone,
    Tablet,
    Desktop,
    TV,
    Watch,
    Car
}
