namespace Shiny.LiveActivities;


/// <summary>
/// The implementation used on platforms with no live activity concept (Windows, macOS, Linux, Blazor).
/// Every call is a safe no-op so shared view models don't need platform checks — branch on
/// <see cref="ILiveActivityManager.IsSupported"/> if the UI should hide the feature entirely.
/// </summary>
public class NoOpLiveActivityManager : ILiveActivityManager
{
    /// <inheritdoc />
    public bool IsSupported => false;

    /// <inheritdoc />
    public string? PushToStartToken => null;

    /// <inheritdoc />
    public Task<AccessState> GetCurrentAccess() => Task.FromResult(AccessState.NotSupported);

    /// <inheritdoc />
    public Task<AccessState> RequestAccess(CancellationToken cancelToken = default) => Task.FromResult(AccessState.NotSupported);

    /// <inheritdoc />
    public IReadOnlyList<LiveActivity> GetAll() => [];

    /// <inheritdoc />
    public Task<LiveActivity> Start(LiveActivityRequest request, CancellationToken cancelToken = default)
        => throw new NotSupportedException("Live activities are not supported on this platform - check ILiveActivityManager.IsSupported first");

    /// <inheritdoc />
    public Task Update(string activityId, LiveActivityContent content, LiveActivityAlert? alert = null, CancellationToken cancelToken = default)
        => Task.CompletedTask;

    /// <inheritdoc />
    public Task End(string activityId, LiveActivityContent? content = null, DateTimeOffset? dismissAt = null, CancellationToken cancelToken = default)
        => Task.CompletedTask;

    /// <inheritdoc />
    public Task EndAll(CancellationToken cancelToken = default) => Task.CompletedTask;
}
