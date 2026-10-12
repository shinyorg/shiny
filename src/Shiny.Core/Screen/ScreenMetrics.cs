namespace Shiny;


/// <summary>
/// A snapshot of the main display, as reported by <see cref="IScreenInfo.Metrics"/>.
/// </summary>
/// <param name="Width">Width in physical pixels, as currently oriented.</param>
/// <param name="Height">Height in physical pixels, as currently oriented.</param>
/// <param name="Density">Physical pixels per device-independent unit (Android dp, Apple point, Windows and CSS pixel at 100%).</param>
/// <param name="Orientation">Whether the app is displayed portrait or landscape.</param>
/// <param name="RefreshRate">The display's refresh rate in hertz, or 0 where the platform does not report one.</param>
public record ScreenMetrics(
    double Width,
    double Height,
    double Density,
    DeviceOrientation Orientation,
    double RefreshRate
)
{
    /// <summary>
    /// Nothing known - every value zero and the orientation <see cref="DeviceOrientation.Unknown"/>.
    /// </summary>
    public static ScreenMetrics Empty { get; } = new(0, 0, 0, DeviceOrientation.Unknown, 0);


    /// <summary>
    /// Width in device-independent units, or 0 when the density is unknown.
    /// </summary>
    public double WidthIndependent => this.Density > 0 ? this.Width / this.Density : 0;


    /// <summary>
    /// Height in device-independent units, or 0 when the density is unknown.
    /// </summary>
    public double HeightIndependent => this.Density > 0 ? this.Height / this.Density : 0;


    internal static DeviceOrientation OrientationOf(double width, double height)
    {
        if (width <= 0 || height <= 0)
            return DeviceOrientation.Unknown;

        return width > height ? DeviceOrientation.Landscape : DeviceOrientation.Portrait;
    }
}
