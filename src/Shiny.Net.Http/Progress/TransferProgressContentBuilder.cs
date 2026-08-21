using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;

namespace Shiny.Net.Http;


/// <summary>
/// Turns a <see cref="TransferProgressSnapshot"/> into the <see cref="TransferProgressContent"/> every
/// renderer draws.
/// </summary>
/// <remarks>
/// Public and static so the exact strings and the exact progress projection can be unit tested, and so an
/// app driving its own surface can reuse the formatting. This is the single source of wording - iOS and
/// Android say the same thing because they call the same method, not because two implementations were kept
/// in sync by hand.
/// </remarks>
public static class TransferProgressContentBuilder
{
    const string Separator = " · ";


    /// <summary>Builds the content for a snapshot.</summary>
    /// <param name="snapshot">The transfers the surface covers.</param>
    /// <param name="options">The configured field selection and projection behaviour.</param>
    /// <param name="progressDelegate">Optional delegate overriding any of the text.</param>
    /// <param name="now">The instant to project progress from. Defaults to <see cref="DateTimeOffset.UtcNow"/>.</param>
    public static TransferProgressContent Build(
        TransferProgressSnapshot snapshot,
        TransferProgressOptions options,
        ITransferProgressDelegate? progressDelegate = null,
        DateTimeOffset? now = null
    )
    {
        var at = now ?? DateTimeOffset.UtcNow;
        var data = options.IncludeRawData ? BuildData(snapshot) : new Dictionary<string, string>();
        progressDelegate?.OnContentBuilding(snapshot, data);

        var title = progressDelegate?.GetTitle(snapshot) ?? BuildTitle(snapshot, options);
        var body = progressDelegate?.GetBody(snapshot) ?? BuildBody(snapshot, options);

        return new TransferProgressContent
        {
            Title = title,
            Body = body,
            ShortStatus = progressDelegate?.GetShortStatus(snapshot) ?? BuildShortStatus(snapshot, options),
            Progress = BuildProgress(snapshot, options, at),
            StaleDate = snapshot.IsTerminal || options.StaleAfter == null ? null : at.Add(options.StaleAfter.Value),
            RelevanceScore = options.RankByProgress ? snapshot.Fraction : null,
            Alert = options.AlertOnCompletion && snapshot.Status == HttpTransferState.Completed
                ? new TransferProgressAlert(title, body)
                : null,
            Data = data
        };
    }


    /// <summary>
    /// Builds the progress a renderer draws.
    /// </summary>
    /// <remarks>
    /// The interesting case is <see cref="TransferProgressOptions.ProjectTimeRemaining"/>. A time range is
    /// emitted rather than a fraction so an iOS Live Activity keeps advancing while the app is suspended and
    /// no progress callbacks are arriving. The range start is anchored in the <em>past</em>, at the point a
    /// constant-rate transfer would have begun, so the bar already sits at the true fraction right now -
    /// anchoring it at "now" would snap the bar back to zero on every single update.
    /// </remarks>
    /// <param name="snapshot">The transfers the surface covers.</param>
    /// <param name="options">The configured projection behaviour.</param>
    /// <param name="now">The instant to project from.</param>
    public static TransferProgressBar BuildProgress(
        TransferProgressSnapshot snapshot,
        TransferProgressOptions options,
        DateTimeOffset now
    )
    {
        var fraction = snapshot.Fraction;
        if (fraction == null)
            return TransferProgressBar.Unknown;

        if (!options.ProjectTimeRemaining || snapshot.Status != HttpTransferState.InProgress)
            return TransferProgressBar.FromValue(fraction.Value);

        var remaining = snapshot.Progress.EstimatedTimeRemaining;
        if (remaining <= TimeSpan.Zero || remaining > options.MaximumProjection || fraction.Value >= 1d)
            return TransferProgressBar.FromValue(fraction.Value);

        // remaining covers the (1 - fraction) still to go, so the whole transfer spans
        // remaining / (1 - fraction) and started fraction of that ago.
        var total = remaining.TotalSeconds / Math.Max(1d - fraction.Value, 0.0001d);
        if (Double.IsNaN(total) || Double.IsInfinity(total) || total > options.MaximumProjection.TotalSeconds * 4)
            return TransferProgressBar.FromValue(fraction.Value);

        return TransferProgressBar.FromRange(
            now.AddSeconds(-(total * fraction.Value)),
            now.Add(remaining)
        );
    }


    /// <summary>Builds the headline text.</summary>
    /// <param name="snapshot">The transfers the surface covers.</param>
    /// <param name="options">The configured field selection.</param>
    public static string BuildTitle(TransferProgressSnapshot snapshot, TransferProgressOptions options)
    {
        var verb = Verb(snapshot);

        switch (snapshot.Status)
        {
            case HttpTransferState.Completed:
                return snapshot.Count > 1 ? $"{snapshot.Count} transfers complete" : $"{verb.Past} complete";

            case HttpTransferState.Error:
                return snapshot.Count > 1 ? "Transfer failed" : $"{verb.Past} failed";

            case HttpTransferState.Canceled:
                return snapshot.Count > 1 ? "Transfers cancelled" : $"{verb.Past} cancelled";

            case HttpTransferState.PausedByNoNetwork:
                return "Waiting for a connection";

            case HttpTransferState.PausedByCostedNetwork:
                return "Waiting for Wi-Fi";

            case HttpTransferState.Paused:
                return snapshot.Count > 1 ? "Transfers paused" : $"{verb.Present} paused";

            case HttpTransferState.Pending:
                return snapshot.Count > 1 ? $"{snapshot.Count} transfers queued" : $"{verb.Present} queued";
        }

        if (snapshot.Count > 1)
            return $"{verb.Present} {snapshot.Count} files";

        var name = options.Fields.HasFlag(TransferProgressFields.FileName) ? snapshot.FileName : null;
        if (!options.Fields.HasFlag(TransferProgressFields.Direction))
            return name ?? "Transferring";

        return name == null ? verb.Present : $"{verb.Present} {name}";
    }


    /// <summary>Builds the supporting detail line, or null when no selected field has anything to say.</summary>
    /// <remarks>
    /// Percent is included here only when it is not already the short status, so it is not printed twice on
    /// a surface that shows both.
    /// </remarks>
    /// <param name="snapshot">The transfers the surface covers.</param>
    /// <param name="options">The configured field selection.</param>
    public static string? BuildBody(TransferProgressSnapshot snapshot, TransferProgressOptions options)
    {
        if (snapshot.Status == HttpTransferState.Error)
            return snapshot.Transfers.Select(x => x.Exception?.Message).FirstOrDefault(x => !String.IsNullOrWhiteSpace(x));

        var parts = new List<string>(5);

        if (options.Fields.HasFlag(TransferProgressFields.Host) && snapshot.Count == 1 && snapshot.Host is { } host)
            parts.Add(host);

        if (options.Fields.HasFlag(TransferProgressFields.Percent) &&
            options.ShortStatus != TransferProgressShortStatus.Percent &&
            snapshot.Fraction is { } fraction)
            parts.Add(FormatPercent(fraction));

        if (options.Fields.HasFlag(TransferProgressFields.TransferredBytes))
        {
            var moved = FormatBytes(snapshot.Progress.BytesTransferred);
            var total = snapshot.Progress.BytesToTransfer;
            parts.Add(
                total.HasValue && total.Value > 0
                    ? $"{moved} of {FormatBytes(total.Value)}"
                    : moved
            );
        }

        if (snapshot.Status == HttpTransferState.InProgress)
        {
            if (options.Fields.HasFlag(TransferProgressFields.Speed) && snapshot.Progress.BytesPerSecond > 0)
                parts.Add(FormatRate(snapshot.Progress.BytesPerSecond));

            if (options.Fields.HasFlag(TransferProgressFields.TimeRemaining) &&
                options.ShortStatus != TransferProgressShortStatus.TimeRemaining &&
                snapshot.Progress.EstimatedTimeRemaining > TimeSpan.Zero)
                parts.Add($"{FormatDuration(snapshot.Progress.EstimatedTimeRemaining)} left");
        }

        return parts.Count == 0 ? null : String.Join(Separator, parts);
    }


    /// <summary>Builds the Dynamic Island compact / status bar chip text, or null.</summary>
    /// <param name="snapshot">The transfers the surface covers.</param>
    /// <param name="options">The configured short status selection.</param>
    public static string? BuildShortStatus(TransferProgressSnapshot snapshot, TransferProgressOptions options)
        => options.ShortStatus switch
        {
            TransferProgressShortStatus.Percent
                => snapshot.Fraction is { } f ? FormatPercent(f) : null,

            TransferProgressShortStatus.TimeRemaining
                => snapshot.Status == HttpTransferState.InProgress && snapshot.Progress.EstimatedTimeRemaining > TimeSpan.Zero
                    ? FormatDuration(snapshot.Progress.EstimatedTimeRemaining, abbreviated: true)
                    : null,

            TransferProgressShortStatus.Speed
                => snapshot.Status == HttpTransferState.InProgress && snapshot.Progress.BytesPerSecond > 0
                    ? FormatRate(snapshot.Progress.BytesPerSecond)
                    : null,

            _ => null
        };


    /// <summary>
    /// The culture-invariant values written to <see cref="TransferProgressContent.Data"/> for a custom
    /// renderer or iOS widget to format itself.
    /// </summary>
    /// <param name="snapshot">The transfers the surface covers.</param>
    public static Dictionary<string, string> BuildData(TransferProgressSnapshot snapshot)
    {
        var data = new Dictionary<string, string>(10)
        {
            ["state"] = snapshot.Status.ToString(),
            ["count"] = snapshot.Count.ToString(CultureInfo.InvariantCulture),
            ["direction"] = snapshot.IsUpload ? "upload" : snapshot.IsDownload ? "download" : "mixed",
            ["bytes"] = snapshot.Progress.BytesTransferred.ToString(CultureInfo.InvariantCulture),
            ["bps"] = snapshot.Progress.BytesPerSecond.ToString(CultureInfo.InvariantCulture)
        };

        if (snapshot.Progress.BytesToTransfer is { } total)
            data["total"] = total.ToString(CultureInfo.InvariantCulture);

        if (snapshot.Fraction is { } fraction)
            data["percent"] = fraction.ToString("0.####", CultureInfo.InvariantCulture);

        if (snapshot.Progress.EstimatedTimeRemaining > TimeSpan.Zero)
            data["etaSeconds"] = ((long)snapshot.Progress.EstimatedTimeRemaining.TotalSeconds).ToString(CultureInfo.InvariantCulture);

        if (!snapshot.IsSummary)
        {
            data["transferId"] = snapshot.Primary.Request.Identifier;
            data["uri"] = snapshot.Primary.Request.Uri;

            if (snapshot.FileName is { } name)
                data["fileName"] = name;
        }
        return data;
    }


    /// <summary>Formats a byte count as a short human string ("12 MB").</summary>
    /// <param name="bytes">The byte count.</param>
    public static string FormatBytes(long bytes)
    {
        if (bytes < 0)
            bytes = 0;

        if (bytes < 1024)
            return bytes.ToString(CultureInfo.InvariantCulture) + " B";

        string[] units = ["KB", "MB", "GB", "TB", "PB"];
        var value = bytes / 1024d;
        var unit = 0;

        while (value >= 1024d && unit < units.Length - 1)
        {
            value /= 1024d;
            unit++;
        }

        // one decimal below 10, none above, so the string never jitters in width
        var format = value < 10d ? "0.0" : "0";
        return value.ToString(format, CultureInfo.InvariantCulture) + " " + units[unit];
    }


    /// <summary>Formats a throughput as bytes per second ("1.5 MB/s").</summary>
    /// <param name="bytesPerSecond">The throughput.</param>
    public static string FormatRate(long bytesPerSecond) => FormatBytes(bytesPerSecond) + "/s";


    /// <summary>Formats a fraction (0.0 - 1.0) as a whole percent ("41%").</summary>
    /// <param name="fraction">The completed fraction.</param>
    public static string FormatPercent(double fraction)
        => Math.Round(Math.Clamp(fraction, 0d, 1d) * 100d).ToString("0", CultureInfo.InvariantCulture) + "%";


    /// <summary>Formats a duration compactly ("45s", "4m 12s", "1h 20m").</summary>
    /// <param name="value">The duration.</param>
    /// <param name="abbreviated">Drop the second component - for the tightest surfaces.</param>
    public static string FormatDuration(TimeSpan value, bool abbreviated = false)
    {
        if (value < TimeSpan.FromSeconds(1))
            return "<1s";

        if (value < TimeSpan.FromMinutes(1))
            return $"{value.Seconds}s";

        if (value < TimeSpan.FromHours(1))
        {
            var minutes = (int)value.TotalMinutes;
            return abbreviated || value.Seconds == 0 ? $"{minutes}m" : $"{minutes}m {value.Seconds}s";
        }

        var hours = (int)value.TotalHours;
        return abbreviated || value.Minutes == 0 ? $"{hours}h" : $"{hours}h {value.Minutes}m";
    }


    static (string Present, string Past) Verb(TransferProgressSnapshot snapshot)
    {
        if (snapshot.IsUpload)
            return ("Uploading", "Upload");

        if (snapshot.IsDownload)
            return ("Downloading", "Download");

        return ("Transferring", "Transfer");
    }
}
