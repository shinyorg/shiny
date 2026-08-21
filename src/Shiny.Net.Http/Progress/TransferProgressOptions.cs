using System;

namespace Shiny.Net.Http;


/// <summary>
/// The pieces of a transfer that may appear on the progress surface - an iOS Live Activity or the Android
/// foreground-service notification. Anything not selected is simply not written into the content, and a
/// renderer draws only what it is given.
/// </summary>
/// <remarks>
/// This gates the human-readable <see cref="TransferProgressContent.Title"/> and
/// <see cref="TransferProgressContent.Body"/> only. The machine-readable values in
/// <see cref="TransferProgressContent.Data"/> are governed separately by
/// <see cref="TransferProgressOptions.IncludeRawData"/>, so a custom iOS widget can render its own layout
/// regardless of what is selected here.
/// </remarks>
[Flags]
public enum TransferProgressFields
{
    /// <summary>Nothing - title and body are left entirely to the delegate.</summary>
    None = 0,

    /// <summary>The local file name ("receipt.pdf").</summary>
    FileName = 1,

    /// <summary>An "Uploading"/"Downloading" verb.</summary>
    Direction = 2,

    /// <summary>Percent complete ("41%").</summary>
    Percent = 4,

    /// <summary>Bytes moved against the total ("12 MB of 48 MB").</summary>
    TransferredBytes = 8,

    /// <summary>Current throughput ("1.5 MB/s").</summary>
    Speed = 16,

    /// <summary>Estimated time remaining ("4m 12s left").</summary>
    TimeRemaining = 32,

    /// <summary>The remote host ("uploads.example.com").</summary>
    Host = 64,

    /// <summary>File name, direction, percent, bytes, speed and time remaining. The default.</summary>
    Default = FileName | Direction | Percent | TransferredBytes | Speed | TimeRemaining,

    /// <summary>Everything, including the host.</summary>
    All = Default | Host
}


/// <summary>
/// What to show in the tightest surfaces - the iOS Dynamic Island compact view and the Android 16 status
/// bar chip. There is room for a handful of characters, so exactly one value is picked.
/// </summary>
public enum TransferProgressShortStatus
{
    /// <summary>Percent complete ("41%").</summary>
    Percent,

    /// <summary>Time remaining, abbreviated ("4m").</summary>
    TimeRemaining,

    /// <summary>Throughput ("1.5 MB/s").</summary>
    Speed,

    /// <summary>Nothing - the compact surface falls back to its icon.</summary>
    None
}


/// <summary>How many progress surfaces to run when several transfers are in flight.</summary>
public enum TransferProgressScope
{
    /// <summary>
    /// One surface covering every running transfer, showing aggregate progress ("3 transfers - 41%").
    /// The default, and the only sane option on Android, where the foreground-service notification is a
    /// single notification by definition.
    /// </summary>
    Summary,

    /// <summary>
    /// One surface per transfer. iOS only in practice - on Android every transfer would fight over the
    /// one foreground-service notification, so the renderer there falls back to
    /// <see cref="Summary"/> behaviour.
    /// </summary>
    PerTransfer
}


/// <summary>
/// Controls how background transfers are projected onto a progress surface.
/// </summary>
/// <remarks>
/// Everything here is about <em>what data is produced</em>, never about layout: an iOS activity is SwiftUI
/// living in the app's widget extension and cannot be driven from C#. Turning fields off keeps them out of
/// the content, and each renderer draws only what is present.
/// </remarks>
public class TransferProgressOptions
{
    /// <summary>
    /// Whether to run one surface for all transfers or one per transfer. Defaults to
    /// <see cref="TransferProgressScope.Summary"/>.
    /// </summary>
    public TransferProgressScope Scope { get; set; } = TransferProgressScope.Summary;

    /// <summary>The fields composed into the title and body. Defaults to <see cref="TransferProgressFields.Default"/>.</summary>
    public TransferProgressFields Fields { get; set; } = TransferProgressFields.Default;

    /// <summary>What the Dynamic Island compact view / status bar chip shows. Defaults to percent.</summary>
    public TransferProgressShortStatus ShortStatus { get; set; } = TransferProgressShortStatus.Percent;

    /// <summary>
    /// The floor between two rendered updates. Transfer progress callbacks fire far more often than any
    /// surface should be redrawn, so updates are coalesced. Defaults to one second.
    /// </summary>
    public TimeSpan MinimumUpdateInterval { get; set; } = TimeSpan.FromSeconds(1);

    /// <summary>
    /// How much progress must move before an update is worth rendering, as a fraction. Applies in addition
    /// to <see cref="MinimumUpdateInterval"/>; a state change always renders. Defaults to 1%.
    /// </summary>
    public double MinimumPercentChange { get; set; } = 0.01;

    /// <summary>
    /// Emit progress as a time range the platform animates on its own, instead of a fixed fraction.
    /// Strongly recommended on iOS and on by default.
    /// </summary>
    /// <remarks>
    /// A background <c>NSURLSession</c> delivers no progress callbacks while the app is suspended, so a
    /// fraction-based bar freezes the moment iOS suspends the process and only moves again when the
    /// transfer completes. A time range keeps advancing with no app involvement, and each real callback
    /// re-anchors it. Android does not have this problem - its foreground service is alive throughout - so
    /// the Android renderer resolves the range back to a fraction. See <see cref="MaximumProjection"/> for
    /// the guard against absurd projections.
    /// </remarks>
    public bool ProjectTimeRemaining { get; set; } = true;

    /// <summary>
    /// The longest projection <see cref="ProjectTimeRemaining"/> will emit. A stalled transfer produces a
    /// nonsense estimate, so anything beyond this falls back to a fixed fraction. Defaults to one hour.
    /// </summary>
    public TimeSpan MaximumProjection { get; set; } = TimeSpan.FromHours(1);

    /// <summary>
    /// Also write raw, culture-invariant values (bytes, percent, bytes/sec, seconds remaining, state) into
    /// <see cref="TransferProgressContent.Data"/> so a custom iOS widget can format them itself. On by
    /// default and cheap - the payload stays far inside ActivityKit's 4KB content-state limit.
    /// </summary>
    public bool IncludeRawData { get; set; } = true;

    /// <summary>
    /// How long content stays fresh before a platform may mark it stale. Set null to never go stale.
    /// Defaults to 30 seconds - roughly when a suspended app's numbers stop being believable.
    /// </summary>
    public TimeSpan? StaleAfter { get; set; } = TimeSpan.FromSeconds(30);

    /// <summary>
    /// How long the final "completed"/"failed" state stays on screen after the transfer ends. Defaults to
    /// four seconds. <see cref="TimeSpan.Zero"/> dismisses immediately.
    /// </summary>
    public TimeSpan DismissCompletedAfter { get; set; } = TimeSpan.FromSeconds(4);

    /// <summary>
    /// Alert when a transfer finishes instead of updating silently. Off by default - with
    /// <see cref="TransferProgressScope.Summary"/> this fires once per batch, not once per transfer.
    /// </summary>
    public bool AlertOnCompletion { get; set; }

    /// <summary>
    /// Set the surface's relevance score to its completion fraction, so the nearest-to-done wins the iOS
    /// Dynamic Island when several are running. Only meaningful with
    /// <see cref="TransferProgressScope.PerTransfer"/>. On by default; ignored on Android.
    /// </summary>
    public bool RankByProgress { get; set; } = true;
}
