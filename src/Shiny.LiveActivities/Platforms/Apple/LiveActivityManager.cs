using Foundation;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.Stores;
using Shiny.Hosting;
using ShinyLiveActivities;

namespace Shiny.LiveActivities;


/// <summary>
/// The iOS implementation, driving ActivityKit through the <c>ShinyActivityBridge</c> Swift shim.
/// </summary>
/// <remarks>
/// Observation starts at app launch rather than on first use, because activities outlive the process:
/// an activity started by a push-to-start payload — or one still running from a previous launch — must
/// still raise <see cref="ILiveActivityDelegate.OnPushTokenChanged"/> so the server can address it.
/// </remarks>
public class LiveActivityManager(
    IServiceProvider services,
    [FromKeyedServices(StoreKeys.Default)] IKeyValueStore store,
    ILogger<LiveActivityManager> logger
) : ILiveActivityManager, IShinyStartupTask
{
    const string PushToStartTokenKey = "Shiny.LiveActivities.PushToStartToken";


    /// <inheritdoc />
    public bool IsSupported => ShinyActivityBridge.IsSupported();


    /// <summary>
    /// The last push-to-start token the system issued, cached across launches so a server registration
    /// can be re-sent without waiting for a fresh token.
    /// </summary>
    public string? PushToStartToken
    {
        get => ShinyActivityBridge.PushToStartToken() ?? store.Get<string>(PushToStartTokenKey);
        private set => store.SetOrRemove(PushToStartTokenKey, value);
    }


    /// <summary>
    /// Begins observing ActivityKit. Invoked by the Shiny host at startup — do not call it yourself.
    /// </summary>
    public void Start()
    {
        if (!this.IsSupported)
        {
            logger.LogInformation("Live Activities are not supported on this iOS version");
            return;
        }

        ShinyActivityBridge.StartObserving(
            this.OnNativeStarted,
            this.OnNativeToken,
            this.OnNativePushToStartToken,
            this.OnNativeState
        );
    }


    /// <inheritdoc />
    public Task<AccessState> GetCurrentAccess()
    {
        if (!this.IsSupported)
            return Task.FromResult(AccessState.NotSupported);

        // There is no prompt for Live Activities - the user either left the per-app switch on or not.
        return Task.FromResult(
            ShinyActivityBridge.AreActivitiesEnabled()
                ? AccessState.Available
                : AccessState.Disabled
        );
    }


    /// <inheritdoc />
    public Task<AccessState> RequestAccess(CancellationToken cancelToken = default) => this.GetCurrentAccess();


    /// <inheritdoc />
    public IReadOnlyList<LiveActivity> GetAll()
    {
        if (!this.IsSupported)
            return [];

        var natives = ShinyActivityBridge.ActiveActivities();
        var list = new List<LiveActivity>(natives.Length);

        foreach (var native in natives)
        {
            var id = Value(native, "id");
            if (id is not null)
                list.Add(new LiveActivity(id, ParseState(Value(native, "state")), Value(native, "pushToken")));
        }
        return list;
    }


    /// <inheritdoc />
    public async Task<LiveActivity> Start(LiveActivityRequest request, CancellationToken cancelToken = default)
    {
        this.AssertSupported();

        var id = ShinyActivityBridge.Start(
            LiveActivityContentSchema.AttributesToJson(request),
            LiveActivityContentSchema.ToJson(request.Content),
            ToUnixSeconds(request.Content.StaleDate),
            request.Content.RelevanceScore is { } score ? NSNumber.FromDouble(score) : null,
            request.RequestPushToken,
            out var error
        );

        if (error != null)
            throw new InvalidOperationException($"Failed to start live activity: {error.LocalizedDescription}");

        if (id == null)
            throw new InvalidOperationException("Failed to start live activity - ActivityKit returned no identifier");

        var activity = new LiveActivity(id, LiveActivityState.Active);
        await services
            .RunDelegates<ILiveActivityDelegate>(x => x.OnStarted(activity), logger)
            .ConfigureAwait(false);

        return activity;
    }


    /// <inheritdoc />
    public Task Update(string activityId, LiveActivityContent content, LiveActivityAlert? alert = null, CancellationToken cancelToken = default)
    {
        this.AssertSupported();

        var tcs = new TaskCompletionSource<bool>();
        ShinyActivityBridge.Update(
            activityId,
            LiveActivityContentSchema.ToJson(content),
            ToUnixSeconds(content.StaleDate),
            content.RelevanceScore is { } score ? NSNumber.FromDouble(score) : null,
            alert?.Title,
            alert?.Body,
            error => Complete(tcs, error, "update")
        );
        return tcs.Task;
    }


    /// <inheritdoc />
    public Task End(string activityId, LiveActivityContent? content = null, DateTimeOffset? dismissAt = null, CancellationToken cancelToken = default)
    {
        this.AssertSupported();

        var tcs = new TaskCompletionSource<bool>();
        ShinyActivityBridge.End(
            activityId,
            content == null ? null : LiveActivityContentSchema.ToJson(content),
            ToUnixSeconds(dismissAt),
            error => Complete(tcs, error, "end")
        );
        return tcs.Task;
    }


    /// <inheritdoc />
    public Task EndAll(CancellationToken cancelToken = default)
    {
        if (!this.IsSupported)
            return Task.CompletedTask;

        var tcs = new TaskCompletionSource<bool>();
        ShinyActivityBridge.EndAll(() => tcs.TrySetResult(true));
        return tcs.Task;
    }


    void OnNativeStarted(NSString id)
    {
        var activity = new LiveActivity(id.ToString(), LiveActivityState.Active);
        logger.LogDebug("Live activity started: {ActivityId}", activity.Id);

        services.RunDelegates<ILiveActivityDelegate>(x => x.OnStarted(activity), logger);
    }


    void OnNativeToken(NSString id, NSString token)
    {
        var activityId = id.ToString();
        var value = token.ToString();
        logger.LogDebug("Live activity {ActivityId} push token issued", activityId);

        var activity = new LiveActivity(activityId, LiveActivityState.Active, value);
        services.RunDelegates<ILiveActivityDelegate>(x => x.OnPushTokenChanged(activity, value), logger);
    }


    void OnNativePushToStartToken(NSString token)
    {
        var value = token.ToString();
        if (value == this.PushToStartToken)
            return;

        this.PushToStartToken = value;
        logger.LogDebug("Live activity push-to-start token issued");

        services.RunDelegates<ILiveActivityDelegate>(x => x.OnPushToStartTokenChanged(value), logger);
    }


    void OnNativeState(NSString id, NSString state)
    {
        var activity = new LiveActivity(id.ToString(), ParseState(state.ToString()));
        logger.LogDebug("Live activity {ActivityId} is now {State}", activity.Id, activity.State);

        services.RunDelegates<ILiveActivityDelegate>(x => x.OnStateChanged(activity), logger);
    }


    void AssertSupported()
    {
        if (!this.IsSupported)
            throw new NotSupportedException("Live Activities require iOS 16.2 or later - check ILiveActivityManager.IsSupported first");
    }


    static void Complete(TaskCompletionSource<bool> tcs, NSError? error, string operation)
    {
        if (error == null)
            tcs.TrySetResult(true);
        else
            tcs.TrySetException(new InvalidOperationException($"Failed to {operation} live activity: {error.LocalizedDescription}"));
    }


    static string? Value(NSDictionary<NSString, NSString> dict, string key)
        => dict.TryGetValue(new NSString(key), out var value) ? value?.ToString() : null;


    static NSNumber? ToUnixSeconds(DateTimeOffset? value)
        => value is { } dto ? NSNumber.FromDouble(dto.ToUnixTimeMilliseconds() / 1000d) : null;


    static LiveActivityState ParseState(string? state) => state switch
    {
        "stale" => LiveActivityState.Stale,
        "ended" => LiveActivityState.Ended,
        "dismissed" => LiveActivityState.Dismissed,
        _ => LiveActivityState.Active
    };
}
