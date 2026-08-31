namespace Shiny.ScreenRecorder;


/// <summary>
/// Something that can be recorded - a display, a window, or an application's windows.
/// </summary>
/// <remarks>
/// Only ever obtained from <see cref="IScreenRecorder.GetTargets"/>. <see cref="Id"/> is the
/// platform's own handle and is not stable across reboots (or, for windows, across the window
/// being closed and reopened), so re-enumerate rather than persisting one.
/// </remarks>
public record CaptureTarget
{
    /// <summary>The platform handle - a CoreGraphics display id, an HMONITOR/HWND, a ScreenCaptureKit window id.</summary>
    public required string Id { get; init; }

    /// <summary>What this refers to.</summary>
    public required CaptureTargetKind Kind { get; init; }

    /// <summary>
    /// Something to show the user - the display's name, the window's title, the application's name.
    /// </summary>
    /// <remarks>
    /// Windows frequently have empty or duplicate titles. Pair it with
    /// <see cref="ApplicationName"/> when presenting a list.
    /// </remarks>
    public required string Name { get; init; }

    /// <summary>The owning application, for windows. Null for displays.</summary>
    public string? ApplicationName { get; init; }

    /// <summary>Width in pixels, where the platform reports it.</summary>
    public int? Width { get; init; }

    /// <summary>Height in pixels, where the platform reports it.</summary>
    public int? Height { get; init; }

    /// <summary>Whether this is the primary display. False for windows.</summary>
    public bool IsPrimary { get; init; }
}
