namespace Shiny;


/// <summary>
/// A rectangle in device-independent units (Android dp, Apple points, CSS pixels), as reported by
/// <see cref="IScreenPosture"/>.
/// </summary>
/// <remarks>
/// Not <c>Rect</c>: MAUI imports <c>Microsoft.Maui.Graphics.Rect</c> implicitly, and most Shiny apps import
/// <c>Shiny</c> globally - a second <c>Rect</c> would make every unqualified use ambiguous.
/// </remarks>
public readonly record struct ScreenRect(double X, double Y, double Width, double Height)
{
    public static ScreenRect Empty { get; } = default;

    public double Left => this.X;
    public double Top => this.Y;
    public double Right => this.X + this.Width;
    public double Bottom => this.Y + this.Height;

    /// <summary>
    /// True when the rectangle has no area. A fold reported as a line (zero width or height) is empty but still
    /// has a position - check <see cref="Width"/> and <see cref="Height"/> rather than this to find a fold.
    /// </summary>
    public bool IsEmpty => this.Width <= 0 || this.Height <= 0;


    public override string ToString() => $"{{X={this.X:0.##} Y={this.Y:0.##} W={this.Width:0.##} H={this.Height:0.##}}}";
}


/// <summary>
/// A size in device-independent units (Android dp, Apple points, CSS pixels).
/// </summary>
/// <remarks>Not <c>Size</c>, for the same reason <see cref="ScreenRect"/> is not <c>Rect</c>.</remarks>
public readonly record struct ScreenSize(double Width, double Height)
{
    public override string ToString() => $"{{W={this.Width:0.##} H={this.Height:0.##}}}";
}
