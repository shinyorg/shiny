using System.Collections.Concurrent;
using Android;
using Android.App;
using Android.Content;
using Android.Runtime;
using Microsoft.Extensions.Logging;

namespace Shiny.LiveActivities;


/// <summary>
/// The Android implementation. Android has no ActivityKit; the closest equivalent is the Android 16
/// (API 36) "Live Updates" feature — a promoted ongoing notification that also renders as a status bar
/// chip and on the always-on display.
/// </summary>
/// <remarks>
/// On Android 16+ the notification uses <c>Notification.ProgressStyle</c> with
/// <c>requestPromotedOngoing</c> and <c>setShortCriticalText</c>. Below 16 it degrades to an ordinary
/// ongoing notification with a progress bar — still useful, just without the chip. There is no push
/// token concept here: an Android "activity" is updated by your app (typically from an FCM data message
/// handled by Shiny.Push), so <see cref="PushToStartToken"/> is always null.
/// </remarks>
public class LiveActivityManager(
    IServiceProvider services,
    AndroidPlatform platform,
    ILogger<LiveActivityManager> logger
) : ILiveActivityManager
{
    /// <summary>The notification channel live activities are posted to.</summary>
    public const string ChannelId = "shiny_live_activities";

    readonly ConcurrentDictionary<string, TrackedActivity> activities = new();
    int nextNotificationId = 8100;


    /// <inheritdoc />
    public bool IsSupported => OperatingSystem.IsAndroidVersionAtLeast(26);

    /// <summary>Always null — Android live updates are driven by the app, not by a dedicated push token.</summary>
    public string? PushToStartToken => null;


    /// <inheritdoc />
    public Task<AccessState> GetCurrentAccess()
    {
        if (!this.IsSupported)
            return Task.FromResult(AccessState.NotSupported);

        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            return Task.FromResult(AccessState.Available);

        return Task.FromResult(platform.GetCurrentPermissionStatus(Manifest.Permission.PostNotifications));
    }


    /// <inheritdoc />
    public async Task<AccessState> RequestAccess(CancellationToken cancelToken = default)
    {
        if (!this.IsSupported)
            return AccessState.NotSupported;

        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            return AccessState.Available;

        return await platform
            .RequestAccess(Manifest.Permission.PostNotifications)
            .ConfigureAwait(false);
    }


    /// <inheritdoc />
    public IReadOnlyList<LiveActivity> GetAll()
        => this.activities.Values
            .Select(x => new LiveActivity(x.Id, x.State))
            .ToList();


    /// <inheritdoc />
    public Task<LiveActivity> Start(LiveActivityRequest request, CancellationToken cancelToken = default)
    {
        this.AssertSupported();
        this.EnsureChannel();

        var tracked = new TrackedActivity(
            Guid.NewGuid().ToString(),
            Interlocked.Increment(ref this.nextNotificationId),
            request.Kind
        );
        this.activities[tracked.Id] = tracked;

        this.Post(tracked, request.Content, ongoing: true);
        logger.LogDebug("Live activity {ActivityId} started", tracked.Id);

        var activity = new LiveActivity(tracked.Id, LiveActivityState.Active);
        return this.Notify(x => x.OnStarted(activity)).ContinueWith(_ => activity, cancelToken);
    }


    /// <inheritdoc />
    public Task Update(string activityId, LiveActivityContent content, LiveActivityAlert? alert = null, CancellationToken cancelToken = default)
    {
        if (!this.activities.TryGetValue(activityId, out var tracked))
        {
            logger.LogWarning("No live activity found with id {ActivityId}", activityId);
            return Task.CompletedTask;
        }

        this.Post(tracked, content, ongoing: true, alert);
        return Task.CompletedTask;
    }


    /// <inheritdoc />
    public Task End(string activityId, LiveActivityContent? content = null, DateTimeOffset? dismissAt = null, CancellationToken cancelToken = default)
    {
        if (!this.activities.TryRemove(activityId, out var tracked))
            return Task.CompletedTask;

        var immediate = dismissAt is { } at && at <= DateTimeOffset.UtcNow;

        if (content == null || immediate)
        {
            // Nothing final to show (or it should go now) — just take it down.
            this.Cancel(tracked);
        }
        else
        {
            // Leave a dismissible summary behind, exactly like iOS keeps an ended activity on screen.
            this.Post(tracked, content, ongoing: false);
        }

        var activity = new LiveActivity(activityId, LiveActivityState.Ended);
        return this.Notify(x => x.OnStateChanged(activity));
    }


    /// <inheritdoc />
    public Task EndAll(CancellationToken cancelToken = default)
    {
        foreach (var tracked in this.activities.Values)
            this.Cancel(tracked);

        this.activities.Clear();
        return Task.CompletedTask;
    }


    /// <summary>
    /// Builds the native notification for an activity. Override to customize the look — actions, custom
    /// layouts, icons — while keeping the lifecycle handling here.
    /// </summary>
    /// <param name="content">The content being rendered.</param>
    /// <param name="ongoing">Whether the activity is still running.</param>
    /// <param name="alert">Alert text, when the update should be noisy.</param>
    protected virtual Notification.Builder CreateBuilder(LiveActivityContent content, bool ongoing, LiveActivityAlert? alert)
    {
        var builder = new Notification.Builder(platform.AppContext, ChannelId)
            .SetSmallIcon(platform.GetNotificationIconResource())
            .SetContentTitle(alert?.Title ?? content.Title)
            .SetContentText(alert?.Body ?? content.Body)
            .SetOngoing(ongoing)
            .SetOnlyAlertOnce(alert == null)!;

        if (platform.AppContext.PackageName is { } packageName)
        {
            var launch = platform.AppContext.PackageManager?.GetLaunchIntentForPackage(packageName);
            if (launch != null)
            {
                builder.SetContentIntent(PendingIntent.GetActivity(
                    platform.AppContext,
                    0,
                    launch,
                    PendingIntentFlags.UpdateCurrent | PendingIntentFlags.Immutable
                ));
            }
        }

        this.ApplyProgress(builder, content, ongoing);
        return builder;
    }


    void ApplyProgress(Notification.Builder builder, LiveActivityContent content, bool ongoing)
    {
        var progress = content.Progress;

        if (OperatingSystem.IsAndroidVersionAtLeast(36))
        {
            // Android 16 Live Updates: the status bar chip + always-on display treatment.
            if (ongoing)
                this.TryRequestPromotedOngoing(builder);

            if (content.ShortStatus is { } shortStatus)
                builder.SetShortCriticalText(shortStatus);

            if (progress is not null)
            {
                var style = new Notification.ProgressStyle();
                style.SetProgress(ToPercent(progress));
                style.SetProgressIndeterminate(progress.Indeterminate);
                builder.SetStyle(style);
            }
        }
        else if (progress is not null)
        {
            builder.SetProgress(100, ToPercent(progress), progress.Indeterminate);
        }
    }


    // .NET for Android 36 does not bind Notification.Builder.requestPromotedOngoing yet, so it is invoked
    // directly. Without it the notification still posts — it just doesn't get the status bar chip.
    void TryRequestPromotedOngoing(Notification.Builder builder)
    {
        try
        {
            var cls = JNIEnv.GetObjectClass(builder.Handle);
            var method = JNIEnv.GetMethodID(cls, "requestPromotedOngoing", "(Z)Landroid/app/Notification$Builder;");
            if (method != IntPtr.Zero)
                JNIEnv.CallObjectMethod(builder.Handle, method, new JValue(true));
        }
        catch (Exception ex)
        {
            logger.LogDebug(ex, "requestPromotedOngoing is unavailable - the live activity will post without the status bar chip");
        }
    }


    void Post(TrackedActivity tracked, LiveActivityContent content, bool ongoing, LiveActivityAlert? alert = null)
    {
        var builder = this.CreateBuilder(content, ongoing, alert);
        this.NotificationManager?.Notify(tracked.NotificationId, builder.Build());
        tracked.State = ongoing ? LiveActivityState.Active : LiveActivityState.Ended;
    }


    void Cancel(TrackedActivity tracked)
    {
        this.NotificationManager?.Cancel(tracked.NotificationId);
        tracked.State = LiveActivityState.Dismissed;
    }


    void EnsureChannel()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(26))
            return;

        var manager = this.NotificationManager;
        if (manager == null || manager.GetNotificationChannel(ChannelId) != null)
            return;

        // Importance must be at least Default or Android 16 will refuse to promote the notification.
        var channel = new NotificationChannel(ChannelId, "Live Activities", NotificationImportance.Default);
        channel.Description = "Ongoing updates such as deliveries, timers and scores";
        manager.CreateNotificationChannel(channel);
    }


    NotificationManager? NotificationManager
        => platform.AppContext.GetSystemService(Context.NotificationService) as NotificationManager;


    Task Notify(Func<ILiveActivityDelegate, Task> execute)
        => services.RunDelegates(execute, logger);


    void AssertSupported()
    {
        if (!this.IsSupported)
            throw new NotSupportedException("Live activities require Android 8.0 or later - check ILiveActivityManager.IsSupported first");
    }


    static int ToPercent(LiveActivityProgress progress)
    {
        if (progress.Value is { } value)
            return (int)Math.Clamp(value * 100, 0, 100);

        if (progress is { Start: { } start, End: { } end } && end > start)
        {
            var elapsed = (DateTimeOffset.UtcNow - start).TotalSeconds;
            var total = (end - start).TotalSeconds;
            return (int)Math.Clamp(elapsed / total * 100, 0, 100);
        }
        return 0;
    }


    class TrackedActivity(string id, int notificationId, string? kind)
    {
        public string Id { get; } = id;
        public int NotificationId { get; } = notificationId;
        public string? Kind { get; } = kind;
        public LiveActivityState State { get; set; } = LiveActivityState.Active;
    }
}
