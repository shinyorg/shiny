namespace Shiny.CallDirectory;


/// <summary>
/// The implementation on platforms with no call directory. Every call is a safe no-op so shared code needs no
/// platform checks - branch on <see cref="ICallDirectoryManager.IsSupported"/> to hide the feature.
/// </summary>
public class NoOpCallDirectoryManager : ICallDirectoryManager
{
    /// <inheritdoc />
    public bool IsSupported => false;

    /// <inheritdoc />
    public Task<AccessState> GetCurrentAccess() => Task.FromResult(AccessState.NotSupported);

    /// <inheritdoc />
    public Task<AccessState> RequestAccess(CancellationToken cancelToken = default) => Task.FromResult(AccessState.NotSupported);

    /// <inheritdoc />
    public Task Set(IEnumerable<CallDirectoryEntry> entries, CancellationToken cancelToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task Remove(IEnumerable<long> phoneNumbers, CancellationToken cancelToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task ReplaceAll(IEnumerable<CallDirectoryEntry> entries, CancellationToken cancelToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task Clear(CancellationToken cancelToken = default) => Task.CompletedTask;

    /// <inheritdoc />
    public Task<CallDirectoryEntry?> Get(long phoneNumber, CancellationToken cancelToken = default) => Task.FromResult<CallDirectoryEntry?>(null);

    /// <inheritdoc />
    public Task<int> Count(CancellationToken cancelToken = default) => Task.FromResult(0);

    /// <inheritdoc />
    public Task<CallDirectoryReloadResult> Reload(CancellationToken cancelToken = default) => Task.FromResult(CallDirectoryReloadResult.NotSupported);
}
