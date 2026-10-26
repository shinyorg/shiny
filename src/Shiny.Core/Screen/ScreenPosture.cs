using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;

namespace Shiny;


/// <summary>
/// Whether a fold or hinge crosses the app's window, as reported by <see cref="ScreenPosture.State"/>.
/// </summary>
public enum FoldState
{
    /// <summary>
    /// Nothing crosses the window - a device that does not fold, a foldable shown on its outer screen (closed), or a
    /// window that does not reach the fold.
    /// </summary>
    None,

    /// <summary>
    /// The window spans the fold, but the device is laid flat - one continuous surface.
    /// </summary>
    Flat,

    /// <summary>
    /// The device is part-way open with the fold across the window - a laptop-like tabletop or a book.
    /// </summary>
    HalfOpen
}


/// <summary>
/// The direction the fold runs across the window.
/// </summary>
public enum FoldOrientation
{
    /// <summary>Unknown, or no fold.</summary>
    None,

    /// <summary>The fold runs top to bottom, splitting the window into left and right - book posture.</summary>
    Vertical,

    /// <summary>The fold runs left to right, splitting the window into top and bottom - tabletop posture.</summary>
    Horizontal
}


/// <summary>
/// The physical hinge, where the platform reports one (iOS; Android only while the fold crosses the window).
/// </summary>
public enum HingeStatus
{
    Unknown,
    Closed,
    PartiallyOpen,
    FullyOpen
}


/// <summary>
/// A snapshot of how the app's window sits on a foldable display, as reported by <see cref="IScreenPosture.Current"/>.
/// All rectangles are in window coordinates, in device-independent units (Android dp, Apple points, CSS pixels).
/// </summary>
/// <param name="State">Whether a fold crosses the window, and whether it is bent.</param>
/// <param name="Orientation">Which way the fold runs - <see cref="FoldOrientation.None"/> when there is none.</param>
/// <param name="Fold">
/// Where the fold (or the physical hinge between two screens) is, or null unless it divides the window. A fold that is
/// only a crease is a zero-width (or zero-height) line.
/// </param>
/// <param name="IsSeparating">
/// True when content placed across the fold would be split or hidden - always true while half open, and true for a flat
/// dual-screen device with a physical gap. Lay out two panes when this is true.
/// </param>
/// <param name="Hinge">The physical hinge's status. Use <see cref="State"/> and <see cref="Fold"/> for layout.</param>
/// <param name="HingeAngleDegrees">
/// The hinge angle (iOS only, else null). Its rate and precision are not guaranteed - use it for live effects, never to
/// decide a layout.
/// </param>
/// <param name="Occlusions">
/// Parts of the window hidden by hardware, such as the iPhone Duo's camera housings. Empty on Android and the browser.
/// </param>
public sealed record ScreenPosture(
    FoldState State,
    FoldOrientation Orientation,
    ScreenRect? Fold,
    bool IsSeparating,
    HingeStatus Hinge,
    double? HingeAngleDegrees,
    IReadOnlyList<ScreenRect> Occlusions
)
{
    /// <summary>
    /// No fold, unknown hinge, nothing occluded - what every platform without posture reports.
    /// </summary>
    public static ScreenPosture None { get; } = new(FoldState.None, FoldOrientation.None, null, false, HingeStatus.Unknown, null, []);


    /// <summary>
    /// Half open with the fold running left to right - the bottom half rests on a table.
    /// </summary>
    public bool IsTabletop => this.State == FoldState.HalfOpen && this.Orientation == FoldOrientation.Horizontal;


    /// <summary>
    /// Half open with the fold running top to bottom - held like a book.
    /// </summary>
    public bool IsBook => this.State == FoldState.HalfOpen && this.Orientation == FoldOrientation.Vertical;


    /// <summary>
    /// The two regions of a <paramref name="window"/> either side of the fold - left and right for a vertical fold,
    /// top and bottom for a horizontal one - or null when nothing separates them: no fold, a fold that does not
    /// separate (<see cref="IsSeparating"/> is false), or a fold that lies outside the window.
    /// </summary>
    /// <param name="window">The window's size in the same units as <see cref="Fold"/>.</param>
    public (ScreenRect First, ScreenRect Second)? Segments(ScreenSize window)
    {
        if (!this.IsSeparating || this.Fold is not { } fold || window.Width <= 0 || window.Height <= 0)
            return null;

        switch (this.Orientation)
        {
            case FoldOrientation.Vertical:
                {
                    var left = Math.Max(0, fold.Left);
                    var right = Math.Min(window.Width, fold.Right);
                    if (left <= 0 || right >= window.Width || right < left)
                        return null;

                    return (
                        new ScreenRect(0, 0, left, window.Height),
                        new ScreenRect(right, 0, window.Width - right, window.Height)
                    );
                }

            case FoldOrientation.Horizontal:
                {
                    var top = Math.Max(0, fold.Top);
                    var bottom = Math.Min(window.Height, fold.Bottom);
                    if (top <= 0 || bottom >= window.Height || bottom < top)
                        return null;

                    return (
                        new ScreenRect(0, 0, window.Width, top),
                        new ScreenRect(0, bottom, window.Width, window.Height - bottom)
                    );
                }

            default:
                return null;
        }
    }


    // Occlusions compare by content, so an identical reading is equal and raises no change
    public bool Equals(ScreenPosture? other)
    {
        if (ReferenceEquals(this, other))
            return true;

        return other is not null &&
               this.State == other.State &&
               this.Orientation == other.Orientation &&
               this.Fold == other.Fold &&
               this.IsSeparating == other.IsSeparating &&
               this.Hinge == other.Hinge &&
               this.HingeAngleDegrees == other.HingeAngleDegrees &&
               this.Occlusions.SequenceEqual(other.Occlusions);
    }


    public override int GetHashCode()
    {
        var hash = new HashCode();
        hash.Add(this.State);
        hash.Add(this.Orientation);
        hash.Add(this.Fold);
        hash.Add(this.IsSeparating);
        hash.Add(this.Hinge);
        hash.Add(this.HingeAngleDegrees);
        foreach (var occlusion in this.Occlusions)
            hash.Add(occlusion);

        return hash.ToHashCode();
    }


    bool PrintMembers(StringBuilder builder)
    {
        builder.Append($"State = {this.State}, Orientation = {this.Orientation}, Fold = {this.Fold?.ToString() ?? "null"}, ");
        builder.Append($"IsSeparating = {this.IsSeparating}, Hinge = {this.Hinge}, HingeAngleDegrees = {this.HingeAngleDegrees?.ToString("0.#") ?? "null"}, ");
        builder.Append($"Occlusions = [{string.Join(", ", this.Occlusions)}]");
        return true;
    }
}
