using Android;
using Android.App;
using Android.App.Roles;
using Android.Content;
using Android.Content.PM;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using Microsoft.Extensions.Logging;
using Shiny.CallDirectory.Infrastructure;

namespace Shiny.CallDirectory;


/// <summary>
/// The Android implementation: a SQLite list that <see cref="ShinyCallScreeningService"/> checks on every incoming
/// call. The app must hold the call screening role (Android 10+), which <see cref="RequestAccess"/> asks for.
/// </summary>
public class CallDirectoryManager : ICallDirectoryManager
{
    const string DatabaseFileName = "shiny_calldirectory.db";
    const string CallerIdChannelId = "shiny_calldirectory";

    readonly AndroidPlatform platform;
    readonly CallDirectoryOptions options;
    readonly ILogger logger;
    readonly CallDirectoryStore store;


    /// <summary>Resolved by DI - see <c>AddCallDirectory</c>.</summary>
    public CallDirectoryManager(AndroidPlatform platform, CallDirectoryOptions options, ILogger<CallDirectoryManager> logger)
    {
        this.platform = platform;
        this.options = options;
        this.logger = logger;

        // screening reads entries live on every call, so there is no change log to keep
        this.store = new CallDirectoryStore(System.IO.Path.Combine(platform.AppData.FullName, DatabaseFileName), trackChanges: false);
    }


    /// <summary>Android 10+ on a device whose telephony offers the call screening role.</summary>
    public bool IsSupported => OperatingSystem.IsAndroidVersionAtLeast(29) && this.GetRoles()?.IsRoleAvailable(RoleManager.RoleCallScreening) == true;


    /// <inheritdoc />
    public Task<AccessState> GetCurrentAccess()
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(29))
            return Task.FromResult(AccessState.NotSupported);

        var roles = this.GetRoles();
        if (roles == null || !roles.IsRoleAvailable(RoleManager.RoleCallScreening))
            return Task.FromResult(AccessState.NotSupported);

        return Task.FromResult(roles.IsRoleHeld(RoleManager.RoleCallScreening) ? AccessState.Available : AccessState.Disabled);
    }


    /// <summary>
    /// Asks the user to make this app the call screening app (the system "Set as default caller ID &amp; spam app"
    /// dialog) and returns whether it now holds the role.
    /// </summary>
    public async Task<AccessState> RequestAccess(CancellationToken cancelToken = default)
    {
        var current = await this.GetCurrentAccess().ConfigureAwait(false);
        if (current is AccessState.Available or AccessState.NotSupported || !OperatingSystem.IsAndroidVersionAtLeast(29))
            return current;

        var roles = this.GetRoles()!;
        var intent = roles.CreateRequestRoleIntent(RoleManager.RoleCallScreening);
        var result = await this.platform.StartActivityForResult(intent, cancelToken).ConfigureAwait(false);

        // the role is the truth - Android can also deny without showing anything after repeated refusals
        var held = roles.IsRoleHeld(RoleManager.RoleCallScreening);
        this.logger.LogDebug("Call screening role request returned {Result}; held: {Held}", result.ResultCode, held);
        return held ? AccessState.Available : AccessState.Denied;
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


    /// <summary>
    /// Android checks the list on every call, so edits apply immediately and this only reports whether screening
    /// is active: <see cref="CallDirectoryReloadResult.ExtensionDisabled"/> until the app holds the role.
    /// </summary>
    public async Task<CallDirectoryReloadResult> Reload(CancellationToken cancelToken = default)
    {
        var access = await this.GetCurrentAccess().ConfigureAwait(false);
        return access switch
        {
            AccessState.Available => CallDirectoryReloadResult.Success,
            AccessState.NotSupported => CallDirectoryReloadResult.NotSupported,
            _ => CallDirectoryReloadResult.ExtensionDisabled
        };
    }


    /// <summary>The path of the SQLite file, for diagnostics.</summary>
    public string DatabasePath => this.store.Path;


    // called by ShinyCallScreeningService for each incoming call - it has 5 seconds to answer
    internal async Task<CallScreeningAction> Screen(string? number, CancellationToken cancelToken)
    {
        var normalized = CallScreening.Normalize(number, this.options.DefaultCountryCode);
        var entry = normalized == null ? null : await this.store.Get(normalized.Value, cancelToken).ConfigureAwait(false);
        var action = CallScreening.Decide(entry, this.options.BlockUnlistedCalls);

        if (action == CallScreeningAction.Identify)
            this.ShowCallerId(entry!, number);

        this.logger.LogDebug("Screened {Number}: {Action}", normalized?.ToString() ?? "withheld", action);
        return action;
    }


    void ShowCallerId(CallDirectoryEntry entry, string? number)
    {
        if (!this.options.ShowCallerIdNotification)
            return;

        var context = this.platform.AppContext;
        if (OperatingSystem.IsAndroidVersionAtLeast(33) &&
            ContextCompat.CheckSelfPermission(context, Manifest.Permission.PostNotifications) != Permission.Granted)
        {
            this.logger.LogDebug("No POST_NOTIFICATIONS permission - caller ID '{Label}' not shown", entry.Label);
            return;
        }

        var manager = NotificationManagerCompat.From(context)!;
        if (OperatingSystem.IsAndroidVersionAtLeast(26))
        {
            var channel = new NotificationChannel(CallerIdChannelId, "Caller ID", NotificationImportance.High);
            ((NotificationManager)context.GetSystemService(Context.NotificationService)!).CreateNotificationChannel(channel);
        }

        var builder = new NotificationCompat.Builder(context, CallerIdChannelId);
        builder.SetSmallIcon(context.ApplicationInfo!.Icon);
        builder.SetContentTitle(entry.Label);
        builder.SetContentText(number);
        builder.SetCategory(NotificationCompat.CategoryCall);
        builder.SetPriority(NotificationCompat.PriorityHigh);
        builder.SetAutoCancel(true);
        // gone once the call has stopped ringing
        builder.SetTimeoutAfter(45_000);
        var notification = builder.Build()!;

        manager.Notify(CallerIdChannelId, (int)(entry.PhoneNumber % int.MaxValue), notification);
    }


    RoleManager? GetRoles() => OperatingSystem.IsAndroidVersionAtLeast(29)
        ? this.platform.AppContext.GetSystemService(Context.RoleService) as RoleManager
        : null;
}
