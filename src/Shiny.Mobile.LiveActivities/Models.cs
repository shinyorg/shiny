namespace Shiny.LiveActivities;


/// <summary>Where a live activity is in its lifecycle.</summary>
public enum LiveActivityState
{
    /// <summary>Running and visible.</summary>
    Active,

    /// <summary>Past its stale date — the content is showing but should be treated as out of date.</summary>
    Stale,

    /// <summary>Ended, but still on screen until it is dismissed.</summary>
    Ended,

    /// <summary>Gone from the Lock Screen / notification shade.</summary>
    Dismissed
}


/// <summary>
/// The progress an activity is reporting. Rendered as a bar on both platforms — as whatever the widget
/// draws on iOS, and as the native <c>ProgressStyle</c> bar (plus the status bar chip) on Android 16+.
/// </summary>
/// <remarks>
/// Use <see cref="Value"/> for a known fraction, or <see cref="Start"/>/<see cref="End"/> for a
/// self-advancing timer the system animates without further updates — the second form is strongly
/// preferred for anything time-based, because every push update costs budget.
/// </remarks>
public record LiveActivityProgress
{
    /// <summary>A completed fraction from 0.0 to 1.0.</summary>
    public double? Value { get; init; }

    /// <summary>The start of a time range the system animates between.</summary>
    public DateTimeOffset? Start { get; init; }

    /// <summary>The end of a time range the system animates between.</summary>
    public DateTimeOffset? End { get; init; }

    /// <summary>Show an indeterminate/unknown progress state.</summary>
    public bool Indeterminate { get; init; }

    /// <summary>Progress at a known fraction (0.0 - 1.0).</summary>
    public static LiveActivityProgress FromValue(double value) => new() { Value = value };

    /// <summary>A timer the system advances on its own between two instants.</summary>
    public static LiveActivityProgress FromRange(DateTimeOffset start, DateTimeOffset end) => new() { Start = start, End = end };
}


/// <summary>
/// The dynamic part of a live activity — everything that can change while it runs.
/// </summary>
/// <remarks>
/// This maps to the Swift <c>ShinyActivityAttributes.ContentState</c> on iOS and to the notification
/// content on Android, and it is the same shape a server pushes as <c>content-state</c>. Send the
/// complete content every time; it replaces the previous state rather than merging with it.
/// </remarks>
public record LiveActivityContent
{
    /// <summary>The headline (order status, team names, "Arriving in 5 min").</summary>
    public string? Title { get; init; }

    /// <summary>Supporting detail under the title.</summary>
    public string? Body { get; init; }

    /// <summary>
    /// A very short status for the tightest surfaces — the iOS Dynamic Island compact view and the
    /// Android 16 status bar chip. Keep it to a handful of characters ("5 min", "2-1").
    /// </summary>
    public string? ShortStatus { get; init; }

    /// <summary>Optional progress indicator.</summary>
    public LiveActivityProgress? Progress { get; init; }

    /// <summary>
    /// When this content should be considered out of date, so the widget can render a stale view. The
    /// system marks the activity <see cref="LiveActivityState.Stale"/> at this point.
    /// </summary>
    public DateTimeOffset? StaleDate { get; init; }

    /// <summary>Ranks this activity against the app's others when several are running (iOS).</summary>
    public double? RelevanceScore { get; init; }

    /// <summary>
    /// Free-form values for your own widget to read. Kept string-typed so the payload is identical
    /// whether it came from your app or from a server push.
    /// </summary>
    public IReadOnlyDictionary<string, string> Data { get; init; } = new Dictionary<string, string>();
}


/// <summary>An alerting update — a banner and (on a paired watch) a tap, instead of a silent refresh.</summary>
/// <param name="Title">Alert title.</param>
/// <param name="Body">Alert body.</param>
public record LiveActivityAlert(string Title, string? Body = null);


/// <summary>Everything needed to start a live activity.</summary>
public record LiveActivityRequest
{
    /// <summary>The initial dynamic content.</summary>
    public required LiveActivityContent Content { get; init; }

    /// <summary>
    /// The static attributes — the fields that never change for this activity's lifetime (order number,
    /// team names). On iOS these become <c>ShinyActivityAttributes</c>, which is also what a
    /// push-to-start payload must name as its <c>attributes-type</c>.
    /// </summary>
    public IReadOnlyDictionary<string, string> Attributes { get; init; } = new Dictionary<string, string>();

    /// <summary>
    /// Selects which widget/layout renders this activity, for apps that ship more than one. Your widget
    /// reads it from the attributes; the stock Android renderer uses it only for logging.
    /// </summary>
    public string? Kind { get; init; }

    /// <summary>
    /// Ask the system for a push token so a server can update this activity. The token arrives on
    /// <see cref="ILiveActivityDelegate.OnPushTokenChanged"/> — it is not available synchronously.
    /// </summary>
    public bool RequestPushToken { get; init; } = true;
}


/// <summary>A running (or recently ended) live activity.</summary>
/// <param name="Id">The system identifier, used to update or end it.</param>
/// <param name="State">Its lifecycle state at the time this snapshot was taken.</param>
/// <param name="PushToken">
/// The APNs token that addresses this activity, once the system has issued one. Null on Android, and
/// null on iOS until the token arrives (watch <see cref="ILiveActivityDelegate.OnPushTokenChanged"/>).
/// </param>
public record LiveActivity(string Id, LiveActivityState State, string? PushToken = null);
