using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace Shiny.Net.Http;


/// <summary>
/// Everything a progress surface needs to know about the transfers it represents, at one instant.
/// </summary>
/// <remarks>
/// The same type covers both scopes: <see cref="TransferProgressScope.PerTransfer"/> produces a snapshot of
/// one transfer, <see cref="TransferProgressScope.Summary"/> one whose <see cref="Progress"/> is the
/// aggregate across the batch.
/// </remarks>
/// <param name="Transfers">The transfers this surface covers - never empty.</param>
/// <param name="Status">The state to render. For a summary this is the most significant state present.</param>
/// <param name="Progress">Progress for the surface as a whole, aggregated for a summary.</param>
/// <param name="IsSummary">Whether this snapshot aggregates several transfers.</param>
public record TransferProgressSnapshot(
    IReadOnlyList<HttpTransferResult> Transfers,
    HttpTransferState Status,
    TransferProgress Progress,
    bool IsSummary
)
{
    /// <summary>The transfer a single-transfer surface is about, or the first of a summary.</summary>
    public HttpTransferResult Primary => this.Transfers[0];

    /// <summary>How many transfers this surface covers.</summary>
    public int Count => this.Transfers.Count;

    /// <summary>True when every covered transfer is an upload.</summary>
    public bool IsUpload => this.Transfers.All(x => x.Request.Type.IsUpload());

    /// <summary>True when every covered transfer is a download.</summary>
    public bool IsDownload => this.Transfers.All(x => x.Request.Type == TransferType.Download);

    /// <summary>The local file name of <see cref="Primary"/>, or null when it has no usable path.</summary>
    public string? FileName
    {
        get
        {
            var path = this.Primary.Request.LocalFilePath;
            if (String.IsNullOrWhiteSpace(path))
                return null;

            var name = Path.GetFileName(path);
            return String.IsNullOrWhiteSpace(name) ? null : name;
        }
    }

    /// <summary>The remote host of <see cref="Primary"/>, or null when the URI will not parse.</summary>
    public string? Host
        => Uri.TryCreate(this.Primary.Request.Uri, UriKind.Absolute, out var uri) ? uri.Host : null;

    /// <summary>The completed fraction (0.0 - 1.0), or null when the total size is unknown.</summary>
    public double? Fraction
    {
        get
        {
            var total = this.Progress.BytesToTransfer;
            if (total is null or <= 0)
                return null;

            return Math.Clamp((double)this.Progress.BytesTransferred / total.Value, 0d, 1d);
        }
    }

    /// <summary>Whether <see cref="Status"/> is a terminal state that should retire the surface.</summary>
    public bool IsTerminal => this.Status
        is HttpTransferState.Completed
        or HttpTransferState.Error
        or HttpTransferState.Canceled;


    /// <summary>
    /// Rolls a set of transfers into one snapshot: byte counts and throughput sum, and the total is only
    /// reported when every transfer knows its own size.
    /// </summary>
    /// <param name="results">The transfers to aggregate - never empty.</param>
    /// <param name="status">The state the batch as a whole should report.</param>
    public static TransferProgressSnapshot Aggregate(IReadOnlyList<HttpTransferResult> results, HttpTransferState status)
    {
        long transferred = 0, perSecond = 0, total = 0;
        var totalKnown = true;

        foreach (var result in results)
        {
            transferred += result.Progress.BytesTransferred;
            perSecond += result.Progress.BytesPerSecond;

            if (result.Progress.BytesToTransfer is { } value)
                total += value;
            else
                totalKnown = false;
        }

        return new TransferProgressSnapshot(
            results,
            status,
            new TransferProgress(perSecond, totalKnown ? total : null, transferred),
            IsSummary: true
        );
    }


    /// <summary>
    /// The state a batch reports while it still has transfers in it. In-progress wins - one moving transfer
    /// means the batch is moving - then queued, then whichever paused reason is present.
    /// </summary>
    /// <param name="running">The transfers still in the queue.</param>
    public static HttpTransferState RunningStatus(IReadOnlyList<HttpTransferResult> running)
    {
        var states = running.Select(x => x.Status).ToHashSet();

        if (states.Contains(HttpTransferState.InProgress))
            return HttpTransferState.InProgress;

        if (states.Contains(HttpTransferState.Pending))
            return HttpTransferState.Pending;

        var paused = PausedStates.FirstOrDefault(states.Contains);
        return paused == default ? HttpTransferState.InProgress : paused;
    }


    /// <summary>
    /// The state a finished batch reports: a failure is the headline, otherwise anything that completed,
    /// otherwise the batch was cancelled.
    /// </summary>
    /// <param name="done">The transfers that left the queue.</param>
    public static HttpTransferState TerminalStatus(IReadOnlyList<HttpTransferResult> done)
    {
        var states = done.Select(x => x.Status).ToHashSet();

        if (states.Contains(HttpTransferState.Error))
            return HttpTransferState.Error;

        if (states.Contains(HttpTransferState.Completed))
            return HttpTransferState.Completed;

        return HttpTransferState.Canceled;
    }


    static readonly HttpTransferState[] PausedStates =
    [
        HttpTransferState.PausedByNoNetwork,
        HttpTransferState.PausedByCostedNetwork,
        HttpTransferState.Paused
    ];
}
