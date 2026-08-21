namespace Shiny.LiveActivities;


/// <summary>
/// Starts, updates and ends live activities — iOS Live Activities (ActivityKit) and Android 16 Live
/// Updates — behind one API.
/// </summary>
/// <remarks>
/// The two platforms are genuinely different: iOS renders arbitrary SwiftUI from a widget extension,
/// Android renders a promoted ongoing notification. The shared contract is therefore a typed
/// <see cref="LiveActivityContent"/> — a state, not a UI tree. Anything platform-specific rides in
/// <see cref="LiveActivityContent.Data"/> for your own widget to read.
/// </remarks>
public interface ILiveActivityManager
{
    /// <summary>
    /// Whether this OS version can show live activities at all (iOS 16.1+, Android 8+ for the fallback
    /// notification). False everywhere else, where every call below is a safe no-op.
    /// </summary>
    bool IsSupported { get; }

    /// <summary>
    /// Whether the user currently permits live activities — the iOS per-app Live Activities switch, or
    /// notification permission on Android. Returns <see cref="AccessState.NotSupported"/> when
    /// <see cref="IsSupported"/> is false.
    /// </summary>
    Task<AccessState> GetCurrentAccess();

    /// <summary>
    /// Requests whatever permission the platform needs (notification permission on Android; iOS has no
    /// prompt for Live Activities, so it reports the current setting).
    /// </summary>
    /// <param name="cancelToken">Cancels the request.</param>
    Task<AccessState> RequestAccess(CancellationToken cancelToken = default);

    /// <summary>The activities this app currently has running, newest first.</summary>
    IReadOnlyList<LiveActivity> GetAll();

    /// <summary>
    /// Starts a new activity. On iOS the system caps how many can run and may refuse; on Android the
    /// activity is a promoted ongoing notification.
    /// </summary>
    /// <param name="request">The initial attributes and content.</param>
    /// <param name="cancelToken">Cancels the request.</param>
    /// <returns>The started activity. Its push token arrives later via the delegate.</returns>
    Task<LiveActivity> Start(LiveActivityRequest request, CancellationToken cancelToken = default);

    /// <summary>
    /// Replaces a running activity's content. Silent by default — pass <paramref name="alert"/> to
    /// surface a banner.
    /// </summary>
    /// <param name="activityId">The id from <see cref="Start"/>.</param>
    /// <param name="content">The complete new content (not a delta).</param>
    /// <param name="alert">Optional alerting text.</param>
    /// <param name="cancelToken">Cancels the request.</param>
    Task Update(string activityId, LiveActivityContent content, LiveActivityAlert? alert = null, CancellationToken cancelToken = default);

    /// <summary>
    /// Ends an activity, optionally showing a final state until it is dismissed.
    /// </summary>
    /// <param name="activityId">The id from <see cref="Start"/>.</param>
    /// <param name="content">Optional final content. Omit to keep what is on screen.</param>
    /// <param name="dismissAt">
    /// When it should disappear. Omit for the platform default (iOS keeps an ended activity for up to
    /// four hours); pass a past time to remove it immediately.
    /// </param>
    /// <param name="cancelToken">Cancels the request.</param>
    Task End(string activityId, LiveActivityContent? content = null, DateTimeOffset? dismissAt = null, CancellationToken cancelToken = default);

    /// <summary>Ends every activity this app has running. Useful on logout.</summary>
    /// <param name="cancelToken">Cancels the request.</param>
    Task EndAll(CancellationToken cancelToken = default);

    /// <summary>
    /// The device's push-to-start token (iOS 17.2+), which lets a server start an activity while the app
    /// isn't running. Null on Android, on older iOS, and until the system issues one — watch
    /// <see cref="ILiveActivityDelegate.OnPushToStartTokenChanged"/> rather than polling this.
    /// </summary>
    string? PushToStartToken { get; }
}
