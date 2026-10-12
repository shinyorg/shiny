namespace Shiny;


/// <summary>
/// How the app is displayed, as reported by <see cref="IScreenInfo.Orientation"/> and requested through
/// <see cref="IScreenInfo.SetOrientation"/>.
/// </summary>
/// <remarks>
/// Not <c>ScreenOrientation</c>, which most Android <c>MainActivity</c> attributes already use from
/// <c>Android.Content.PM</c>, nor <c>DisplayOrientation</c>, which MAUI imports implicitly.
/// </remarks>
public enum DeviceOrientation
{
    Unknown,
    Portrait,
    Landscape
}
