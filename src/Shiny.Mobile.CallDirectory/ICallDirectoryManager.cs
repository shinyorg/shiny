namespace Shiny.CallDirectory;


/// <summary>
/// Blocks and identifies incoming calls from a list of numbers the app owns.
/// </summary>
/// <remarks>
/// On iOS the list lives in a SQLite file in the app group container. The system loads it through a Call
/// Directory extension (built by the package, no Xcode project) when <see cref="Reload"/> is called - it never
/// runs app code during a call. Everywhere else today <see cref="IsSupported"/> is false and every call is a no-op.
/// <para>
/// Edits do not reach the system until <see cref="Reload"/>, so a batch of edits costs one extension load.
/// </para>
/// </remarks>
public interface ICallDirectoryManager
{
    /// <summary>Whether this platform can block or identify calls at all.</summary>
    bool IsSupported { get; }

    /// <summary>
    /// iOS: whether the user turned the extension on in Settings > Apps > Phone > Call Blocking &amp; Identification.
    /// </summary>
    Task<AccessState> GetCurrentAccess();

    /// <summary>
    /// iOS has no prompt: this opens Settings (best effort - iOS 26 does not land on the right page) and
    /// returns the current state. Check <see cref="GetCurrentAccess"/> again when the app resumes.
    /// </summary>
    Task<AccessState> RequestAccess(CancellationToken cancelToken = default);

    /// <summary>Adds or replaces entries.</summary>
    Task Set(IEnumerable<CallDirectoryEntry> entries, CancellationToken cancelToken = default);

    /// <summary>Removes entries. Numbers that are not stored are ignored.</summary>
    Task Remove(IEnumerable<long> phoneNumbers, CancellationToken cancelToken = default);

    /// <summary>
    /// Replaces the whole list in one transaction and makes the next load a full one. Use it for a server sync.
    /// </summary>
    Task ReplaceAll(IEnumerable<CallDirectoryEntry> entries, CancellationToken cancelToken = default);

    /// <summary>Removes every entry.</summary>
    Task Clear(CancellationToken cancelToken = default);

    /// <summary>The stored entry for a number, or null.</summary>
    Task<CallDirectoryEntry?> Get(long phoneNumber, CancellationToken cancelToken = default);

    /// <summary>How many entries are stored.</summary>
    Task<int> Count(CancellationToken cancelToken = default);

    /// <summary>
    /// iOS: asks the system to load the extension so it picks up the stored list, and reports how that went.
    /// </summary>
    Task<CallDirectoryReloadResult> Reload(CancellationToken cancelToken = default);
}
