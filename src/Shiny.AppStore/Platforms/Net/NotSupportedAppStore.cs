namespace Shiny.AppStore;


/// <summary>
/// Plain .NET outside Linux has no store - nothing is found and nothing opens, and nothing throws, so
/// shared code needs no platform checks.
/// </summary>
public class NotSupportedAppStore : IAppStore
{
    public Task<AppStoreResult?> GetCurrent(CancellationToken cancellationToken = default) => Task.FromResult<AppStoreResult?>(null);
    public Task<bool> OpenStore() => Task.FromResult(false);
    public Task<bool> OpenReviewPage() => Task.FromResult(false);
    public Task<bool> RequestReview() => Task.FromResult(false);
}
