using CallKit;
using Foundation;
using Microsoft.Extensions.Logging;
using Shiny.CallDirectory.Infrastructure;

namespace Shiny.CallDirectory;


/// <summary>
/// The iOS implementation: writes the shared SQLite store and asks CallKit to load the Call Directory extension.
/// </summary>
public class CallDirectoryManager : ICallDirectoryManager
{
    // the bundle Shiny.Mobile.CallDirectory.targets builds, and the key it stamps into the extension's Info.plist
    const string ExtensionBundleName = "ShinyCallDirectory.appex";
    const string AppGroupInfoKey = "ShinyCallDirectoryAppGroup";

    // written by the extension to the app group's defaults after a load the system accepted
    const string AppliedGenerationKey = "ShinyCallDirectory.AppliedGeneration";
    const string AppliedSeqKey = "ShinyCallDirectory.AppliedSeq";
    const string LastErrorKey = "ShinyCallDirectory.LastError";

    const string DatabaseFileName = "shiny_calldirectory.db";

    readonly ILogger logger;
    readonly string? appGroup;
    readonly string? extensionId;
    readonly CallDirectoryStore store;


    /// <summary>Resolved by DI - see <c>AddCallDirectory</c>.</summary>
    public CallDirectoryManager(CallDirectoryOptions options, ILogger<CallDirectoryManager> logger)
    {
        this.logger = logger;
        var extension = FindExtension();
        this.appGroup = options.AppGroup ?? extension?.ObjectForInfoDictionary(AppGroupInfoKey)?.ToString();
        this.extensionId = options.ExtensionBundleId ?? extension?.BundleIdentifier;

        // No throwing here: an app built off a Mac (Windows, Hot Restart) has no extension bundle, and
        // resolving this service must not take it down. The store still works; IsSupported is false and Reload
        // reports ExtensionNotFound.
        if (this.extensionId == null)
            logger.LogWarning("This app has no Call Directory extension - set <ShinyCallDirectoryExtension>true</ShinyCallDirectoryExtension> and build on a Mac. Entries are stored but iOS never sees them.");

        var container = this.appGroup == null ? null : NSFileManager.DefaultManager.GetContainerUrl(this.appGroup)?.Path;
        if (container == null)
        {
            if (this.appGroup != null)
                logger.LogWarning("The app group '{AppGroup}' has no container - the app is not entitled to it, so the extension cannot read the store. Check the provisioning profile includes the group.", this.appGroup);

            container = Environment.GetFolderPath(Environment.SpecialFolder.LocalApplicationData);
        }
        this.store = new CallDirectoryStore(System.IO.Path.Combine(container, DatabaseFileName));
    }


    /// <inheritdoc />
    public bool IsSupported => this.extensionId != null;


    /// <inheritdoc />
    public async Task<AccessState> GetCurrentAccess()
    {
        if (this.extensionId == null)
            return AccessState.NotSupported;

        try
        {
            var status = await CXCallDirectoryManager.SharedInstance.GetEnabledStatusForExtensionAsync(this.extensionId).ConfigureAwait(false);
            return status switch
            {
                CXCallDirectoryEnabledStatus.Enabled => AccessState.Available,
                CXCallDirectoryEnabledStatus.Disabled => AccessState.Disabled,
                _ => AccessState.Unknown
            };
        }
        catch (NSErrorException ex)
        {
            this.logger.LogWarning("Could not read the Call Directory extension status: {Error}", ex.Error.LocalizedDescription);
            return AccessState.Unknown;
        }
    }


    /// <inheritdoc />
    public async Task<AccessState> RequestAccess(CancellationToken cancelToken = default)
    {
        var current = await this.GetCurrentAccess().ConfigureAwait(false);
        if (current is not AccessState.Available and not AccessState.NotSupported)
        {
            try
            {
                await CXCallDirectoryManager.SharedInstance.OpenSettingsAsync().ConfigureAwait(false);
            }
            catch (NSErrorException ex)
            {
                this.logger.LogWarning("Could not open Call Blocking & Identification settings: {Error}", ex.Error.LocalizedDescription);
            }
        }
        return current;
    }


    /// <inheritdoc />
    public Task Set(IEnumerable<CallDirectoryEntry> entries, CancellationToken cancelToken = default) => this.store.Set(entries, cancelToken);

    /// <inheritdoc />
    public Task Remove(IEnumerable<long> phoneNumbers, CancellationToken cancelToken = default) => this.store.Remove(phoneNumbers, cancelToken);

    /// <inheritdoc />
    public Task ReplaceAll(IEnumerable<CallDirectoryEntry> entries, CancellationToken cancelToken = default) => this.store.ReplaceAll(entries, cancelToken);

    /// <inheritdoc />
    public Task Clear(CancellationToken cancelToken = default) => this.store.Clear(cancelToken);

    /// <inheritdoc />
    public Task<CallDirectoryEntry?> Get(long phoneNumber, CancellationToken cancelToken = default) => this.store.Get(phoneNumber, cancelToken);

    /// <inheritdoc />
    public Task<int> Count(CancellationToken cancelToken = default) => this.store.Count(cancelToken);


    /// <inheritdoc />
    public async Task<CallDirectoryReloadResult> Reload(CancellationToken cancelToken = default)
    {
        if (this.extensionId == null || this.appGroup == null)
            return CallDirectoryReloadResult.ExtensionNotFound;

        using var defaults = new NSUserDefaults(this.appGroup, NSUserDefaultsType.SuiteName);

        // what the extension already applied never needs replaying; trimming keeps every incremental load small
        var appliedGeneration = (long)defaults.IntForKey(AppliedGenerationKey);
        var appliedSeq = (long)defaults.IntForKey(AppliedSeqKey);
        if (appliedGeneration > 0)
            await this.store.Trim(appliedGeneration, appliedSeq, cancelToken).ConfigureAwait(false);

        try
        {
            await CXCallDirectoryManager.SharedInstance.ReloadExtensionAsync(this.extensionId).ConfigureAwait(false);
            this.logger.LogDebug("Call Directory extension reloaded");
            return CallDirectoryReloadResult.Success;
        }
        catch (NSErrorException ex)
        {
            var extensionError = defaults.StringForKey(LastErrorKey);
            var result = ex.Error.Domain == CXErrorCodeCallDirectoryManagerError.Unknown.GetDomain()
                ? Map((CXErrorCodeCallDirectoryManagerError)(long)ex.Error.Code)
                : CallDirectoryReloadResult.Failed;

            this.logger.LogWarning(
                "Call Directory reload failed: {Result} ({Domain} {Code}: {Error}). Extension reported: {ExtensionError}",
                result,
                ex.Error.Domain,
                ex.Error.Code,
                ex.Error.LocalizedDescription,
                extensionError ?? "nothing"
            );
            return result;
        }
    }


    /// <summary>The path of the shared SQLite file, for diagnostics.</summary>
    public string DatabasePath => this.store.Path;


    static CallDirectoryReloadResult Map(CXErrorCodeCallDirectoryManagerError code) => code switch
    {
        CXErrorCodeCallDirectoryManagerError.ExtensionDisabled => CallDirectoryReloadResult.ExtensionDisabled,
        CXErrorCodeCallDirectoryManagerError.NoExtensionFound => CallDirectoryReloadResult.ExtensionNotFound,
        CXErrorCodeCallDirectoryManagerError.CurrentlyLoading => CallDirectoryReloadResult.CurrentlyLoading,
        CXErrorCodeCallDirectoryManagerError.MaximumEntriesExceeded => CallDirectoryReloadResult.MaximumEntriesExceeded,
        CXErrorCodeCallDirectoryManagerError.LoadingInterrupted => CallDirectoryReloadResult.LoadingInterrupted,
        _ => CallDirectoryReloadResult.Failed
    };


    static NSBundle? FindExtension()
    {
        var plugIns = NSBundle.MainBundle.BuiltinPluginsPath;
        return plugIns == null ? null : NSBundle.FromPath(System.IO.Path.Combine(plugIns, ExtensionBundleName));
    }
}
