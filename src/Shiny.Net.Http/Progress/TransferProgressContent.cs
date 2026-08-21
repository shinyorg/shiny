using System;
using System.Collections.Generic;

namespace Shiny.Net.Http;


/// <summary>
/// The progress indicator a renderer should draw.
/// </summary>
/// <remarks>
/// Either a known fraction, or a time range the platform animates on its own without further updates, or
/// indeterminate. The range form is what keeps an iOS Live Activity moving while the app is suspended;
/// renderers that cannot animate a range (the Android notification) resolve it back to a fraction with
/// <see cref="ToFraction"/>.
/// </remarks>
public record TransferProgressBar
{
    /// <summary>A completed fraction from 0.0 to 1.0.</summary>
    public double? Value { get; init; }

    /// <summary>The start of a time range the platform animates between.</summary>
    public DateTimeOffset? Start { get; init; }

    /// <summary>The end of a time range the platform animates between.</summary>
    public DateTimeOffset? End { get; init; }

    /// <summary>Whether the total is unknown and the bar should show an indeterminate state.</summary>
    public bool Indeterminate { get; init; }

    /// <summary>Progress at a known fraction (0.0 - 1.0).</summary>
    /// <param name="value">The completed fraction.</param>
    public static TransferProgressBar FromValue(double value) => new() { Value = value };

    /// <summary>A range the platform advances on its own between two instants.</summary>
    /// <param name="start">When the transfer would have begun at the current rate.</param>
    /// <param name="end">When it is projected to finish.</param>
    public static TransferProgressBar FromRange(DateTimeOffset start, DateTimeOffset end) => new() { Start = start, End = end };

    /// <summary>An indeterminate bar, for a transfer of unknown size.</summary>
    public static TransferProgressBar Unknown { get; } = new() { Indeterminate = true };


    /// <summary>
    /// Collapses this to a plain fraction, resolving a time range against <paramref name="now"/>. For
    /// renderers that draw a static bar rather than animating one.
    /// </summary>
    /// <param name="now">The instant to resolve a range at. Defaults to <see cref="DateTimeOffset.UtcNow"/>.</param>
    /// <returns>The fraction, or null when indeterminate.</returns>
    public double? ToFraction(DateTimeOffset? now = null)
    {
        if (this.Value is { } value)
            return Math.Clamp(value, 0d, 1d);

        if (this.Start is { } start && this.End is { } end && end > start)
        {
            var elapsed = ((now ?? DateTimeOffset.UtcNow) - start).TotalSeconds;
            return Math.Clamp(elapsed / (end - start).TotalSeconds, 0d, 1d);
        }
        return null;
    }
}


/// <summary>An alerting update - a banner or a noisy notification, instead of a silent refresh.</summary>
/// <param name="Title">Alert title.</param>
/// <param name="Body">Alert body.</param>
public record TransferProgressAlert(string Title, string? Body = null);


/// <summary>
/// The platform-neutral description of what a progress surface should show right now.
/// </summary>
/// <remarks>
/// This is the whole contract between <see cref="TransferProgressManager"/> and an
/// <see cref="ITransferProgressRenderer"/>: a <em>state</em>, not a UI tree. The iOS renderer maps it onto
/// an ActivityKit content state; the Android renderer maps it onto the foreground-service notification.
/// Send the complete content every time - it replaces the previous state rather than merging with it.
/// </remarks>
public record TransferProgressContent
{
    /// <summary>The headline ("Uploading receipt.pdf").</summary>
    public string? Title { get; init; }

    /// <summary>Supporting detail under the title ("12 MB of 48 MB - 1.5 MB/s - 23s left").</summary>
    public string? Body { get; init; }

    /// <summary>
    /// A very short status for the tightest surfaces - the iOS Dynamic Island compact view and the Android
    /// 16 status bar chip. A handful of characters at most ("41%").
    /// </summary>
    public string? ShortStatus { get; init; }

    /// <summary>The progress indicator, or null when there is nothing to draw.</summary>
    public TransferProgressBar? Progress { get; init; }

    /// <summary>When this content should be considered out of date, so a renderer can show a stale view.</summary>
    public DateTimeOffset? StaleDate { get; init; }

    /// <summary>Ranks this surface against the app's others when several are running (iOS).</summary>
    public double? RelevanceScore { get; init; }

    /// <summary>Set when this update should alert rather than refresh silently.</summary>
    public TransferProgressAlert? Alert { get; init; }

    /// <summary>
    /// Raw, culture-invariant values for a custom renderer or iOS widget to format itself. Kept
    /// string-typed so the payload is identical whether it came from the app or from a server push.
    /// </summary>
    public IReadOnlyDictionary<string, string> Data { get; init; } = new Dictionary<string, string>();
}
