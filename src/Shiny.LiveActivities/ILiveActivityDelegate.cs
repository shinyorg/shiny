namespace Shiny.LiveActivities;


/// <summary>
/// Callbacks Shiny raises for live activity lifecycle and token events. Register an implementation with
/// <c>AddLiveActivities&lt;TDelegate&gt;()</c>; it is resolved from DI, so it can take dependencies and
/// it runs even when the event arrives with the app backgrounded or freshly relaunched.
/// </summary>
public interface ILiveActivityDelegate
{
    /// <summary>Called when an activity starts — from your code, or from a push-to-start payload.</summary>
    /// <param name="activity">The activity that started.</param>
    Task OnStarted(LiveActivity activity);

    /// <summary>
    /// Called when the system issues or rotates the push token for a single activity. Send it to your
    /// server (as <c>PushTokenKind.LiveActivityUpdate</c>) — it is the only way to update that activity
    /// remotely, and it dies when the activity ends.
    /// </summary>
    /// <param name="activity">The activity the token addresses.</param>
    /// <param name="token">The hex APNs token.</param>
    Task OnPushTokenChanged(LiveActivity activity, string token);

    /// <summary>
    /// Called when the device's push-to-start token appears or rotates (iOS 17.2+). Send it to your
    /// server as <c>PushTokenKind.LiveActivityStart</c>; it survives app launches and lets the server
    /// start an activity with the app closed.
    /// </summary>
    /// <param name="token">The hex APNs token.</param>
    Task OnPushToStartTokenChanged(string token);

    /// <summary>Called when an activity becomes stale, ends, or is dismissed.</summary>
    /// <param name="activity">The activity, carrying its new state.</param>
    Task OnStateChanged(LiveActivity activity);
}


/// <summary>
/// A no-op <see cref="ILiveActivityDelegate"/>. Inherit it and override only what you need — most apps
/// only care about the two token callbacks.
/// </summary>
public abstract class LiveActivityDelegate : ILiveActivityDelegate
{
    /// <inheritdoc />
    public virtual Task OnStarted(LiveActivity activity) => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task OnPushTokenChanged(LiveActivity activity, string token) => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task OnPushToStartTokenChanged(string token) => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task OnStateChanged(LiveActivity activity) => Task.CompletedTask;
}
