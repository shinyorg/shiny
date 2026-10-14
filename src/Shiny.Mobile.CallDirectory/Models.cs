namespace Shiny.CallDirectory;


/// <summary>A number in the call directory.</summary>
/// <param name="PhoneNumber">
/// Country code and digits, no '+' or formatting - 18005550100. <see cref="PhoneNumbers.ToCallDirectory"/> converts.
/// </param>
/// <param name="Label">The caller ID shown for <see cref="CallDirectoryEntryType.Identify"/> and <see cref="CallDirectoryEntryType.Allow"/>.</param>
/// <param name="Type">What happens to calls from this number.</param>
public record CallDirectoryEntry(
    long PhoneNumber,
    string? Label = null,
    CallDirectoryEntryType Type = CallDirectoryEntryType.Identify
);


/// <summary>What happens to calls from a number.</summary>
public enum CallDirectoryEntryType
{
    /// <summary>Calls are blocked.</summary>
    Block = 0,

    /// <summary>Calls ring with <see cref="CallDirectoryEntry.Label"/> as the caller ID.</summary>
    Identify = 1,

    /// <summary>
    /// Android: always allowed, even when unlisted numbers are blocked. iOS has no allow list, so this is
    /// stored and loaded as <see cref="Identify"/> - a label only, no guarantee the call gets through
    /// another blocker or Silence Unknown Callers.
    /// </summary>
    Allow = 2
}


/// <summary>The outcome of <see cref="ICallDirectoryManager.Reload"/>.</summary>
/// <remarks>Android reads the list live on every call, so there it only reports whether screening is active.</remarks>
public enum CallDirectoryReloadResult
{
    /// <summary>The system loaded the list.</summary>
    Success,

    /// <summary>The platform has no call directory.</summary>
    NotSupported,

    /// <summary>iOS: the user has not turned the extension on in Settings. Android: the app does not hold the call screening role.</summary>
    ExtensionDisabled,

    /// <summary>The app bundle has no Call Directory extension - the build did not include it.</summary>
    ExtensionNotFound,

    /// <summary>A load is already running. Try again shortly.</summary>
    CurrentlyLoading,

    /// <summary>The list is larger than the system accepts.</summary>
    MaximumEntriesExceeded,

    /// <summary>The extension ran out of memory or time.</summary>
    LoadingInterrupted,

    /// <summary>Anything else; the error is logged.</summary>
    Failed
}


/// <summary>Call directory configuration.</summary>
public class CallDirectoryOptions
{
    /// <summary>
    /// iOS: the app group the app and the extension share. Leave it null to use the one the build stamped into
    /// the extension's Info.plist from &lt;ShinyCallDirectoryAppGroup&gt;.
    /// </summary>
    public string? AppGroup { get; set; }

    /// <summary>
    /// iOS: the extension's bundle id. Leave it null to use the bundled PlugIns/ShinyCallDirectory.appex.
    /// </summary>
    public string? ExtensionBundleId { get; set; }

    /// <summary>
    /// Android: the country code given to an incoming number that arrives in national format, with no country code
    /// - see <see cref="PhoneNumbers.ToCallDirectory"/>. Defaults to 1.
    /// </summary>
    public int DefaultCountryCode { get; set; } = 1;

    /// <summary>
    /// Android: block every caller that is not in the list, including withheld numbers. <see cref="CallDirectoryEntryType.Allow"/>
    /// and <see cref="CallDirectoryEntryType.Identify"/> entries still ring. Off by default.
    /// </summary>
    public bool BlockUnlistedCalls { get; set; }

    /// <summary>
    /// Android: the dialer's caller ID cannot be changed by another app, so a call from an
    /// <see cref="CallDirectoryEntryType.Identify"/> entry posts a notification with its label while it rings.
    /// Needs POST_NOTIFICATIONS on Android 13+; without it the call simply rings. Defaults to true.
    /// </summary>
    public bool ShowCallerIdNotification { get; set; } = true;
}
