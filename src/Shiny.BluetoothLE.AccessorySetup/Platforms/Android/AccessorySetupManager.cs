using System.Runtime.Versioning;
using Android.Bluetooth.LE;
using Android.Companion;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Microsoft.Extensions.Logging;
using Shiny.BluetoothLE.AccessorySetup.Infrastructure;
using Pattern = Java.Util.Regex.Pattern;
using NativeScanResult = Android.Bluetooth.LE.ScanResult;

namespace Shiny.BluetoothLE.AccessorySetup;


/// <summary>
/// <see cref="IAccessorySetupManager"/> over CompanionDeviceManager. The user's choice in the system dialog creates an
/// <b>association</b> between the app and the device, which is what "granted" means here: the app can scan for and
/// connect to it without location permission, and (Android 12+) be woken when it comes into range.
/// <para>
/// GATT still needs <c>BLUETOOTH_CONNECT</c> on Android 12+ - CompanionDeviceManager replaces the scan permission,
/// not the connect one.
/// </para>
/// </summary>
public class AccessorySetupManager(
    AndroidPlatform platform,
    IServiceProvider services,
    ILogger<AccessorySetupManager> logger
) : IAccessorySetupManager, IShinyStartupTask
{
    // On Android 13+ the association arrives through both the activity result and onAssociationCreated, in either order
    static readonly TimeSpan AssociationCallbackTimeout = TimeSpan.FromSeconds(5);

    int pickerActive;


    CompanionDeviceManager Cdm => platform.GetSystemService<CompanionDeviceManager>(Context.CompanionDeviceService);


    /// <inheritdoc />
    public bool IsSupported => platform.AppContext.PackageManager?.HasSystemFeature(PackageManager.FeatureCompanionDeviceSetup) ?? false;

    /// <inheritdoc />
    public bool IsPresenceSupported => this.IsSupported && OperatingSystem.IsAndroidVersionAtLeast(31);


    /// <inheritdoc />
    public void Start() => Current = this;


    // what ShinyCompanionDeviceService, which the system creates, hands its events to
    internal static AccessorySetupManager? Current { get; private set; }


    /// <inheritdoc />
    public Task<IReadOnlyList<SetupAccessory>> GetAccessories(CancellationToken cancelToken = default)
    {
        if (!this.IsSupported)
            return Task.FromResult<IReadOnlyList<SetupAccessory>>([]);

        var list = new List<SetupAccessory>();
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            foreach (var info in this.Cdm.MyAssociations ?? [])
            {
                if (info != null)
                    list.Add(ToAccessory(info, null));
            }
        }
        else
        {
#pragma warning disable CA1422 // the address list is the only form before Android 13
            foreach (var address in this.Cdm.Associations ?? [])
            {
                if (address != null)
                    list.Add(ToAccessory(address, null));
            }
#pragma warning restore CA1422
        }
        return Task.FromResult<IReadOnlyList<SetupAccessory>>(list);
    }


    /// <inheritdoc />
    public async Task<SetupAccessory?> ShowPicker(
        IReadOnlyList<AccessoryPickerItem> items,
        AccessoryPickerOptions? options = null,
        CancellationToken cancelToken = default
    )
    {
        AccessoryFilterValidator.Validate(items);
        this.AssertSupported();

        if (Interlocked.Exchange(ref this.pickerActive, 1) == 1)
            throw new AccessorySetupException(AccessorySetupErrorCode.PickerAlreadyActive, "The companion device picker is already showing");

        try
        {
            var builder = new AssociationRequest.Builder();
            foreach (var item in items)
                builder.AddDeviceFilter(ToDeviceFilter(item.Filter));

            builder.SetSingleDevice(options?.AndroidSingleDevice ?? false);
            var request = builder.Build();

            var callback = new AssociateCallback(logger);
            if (OperatingSystem.IsAndroidVersionAtLeast(33))
            {
                this.Cdm.Associate(request, platform.AppContext.MainExecutor!, callback);
            }
            else
            {
#pragma warning disable CA1422 // the Handler overload is the only one before Android 13
                this.Cdm.Associate(request, callback, new Handler(Looper.MainLooper!));
#pragma warning restore CA1422
            }

            // the system searches first; what comes back is the consent dialog to launch, or null when nothing was
            // found or the user backed out before it appeared
            IntentSender? sender;
            using (cancelToken.Register(() => callback.Pending.TrySetCanceled(cancelToken)))
                sender = await callback.Pending.Task.ConfigureAwait(false);

            if (sender == null)
            {
                logger.LogDebug("Companion device search ended without a device: {Reason}", callback.FailureReason);
                return null;
            }

            var result = await platform.StartIntentSenderForResult(sender, cancelToken).ConfigureAwait(false);
            if (!result.IsOk)
                return null;

            var fallbackName = items.Count == 1 ? items[0].Name : null;
            var accessory = await this.ReadResult(result.Data, callback, fallbackName, cancelToken).ConfigureAwait(false);
            if (accessory != null)
                _ = services.RunDelegates<IAccessorySetupDelegate>(x => x.OnAccessoryAdded(accessory), logger);

            return accessory;
        }
        finally
        {
            Interlocked.Exchange(ref this.pickerActive, 0);
        }
    }


    /// <inheritdoc />
    public Task Remove(SetupAccessory accessory, CancellationToken cancelToken = default)
    {
        ArgumentNullException.ThrowIfNull(accessory);
        this.AssertSupported();

        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(33) && Int32.TryParse(accessory.Identifier, out var id))
            {
                this.Cdm.Disassociate(id);
            }
            else
            {
                var address = accessory.MacAddress ?? accessory.Identifier;
#pragma warning disable CA1422 // the address form is the only one before Android 13
                this.Cdm.Disassociate(address);
#pragma warning restore CA1422
            }
        }
        catch (Exception ex) when (ex is Java.Lang.IllegalArgumentException)
        {
            throw new AccessorySetupException(AccessorySetupErrorCode.NotAssociated, $"Accessory '{accessory.Identifier}' is not associated with this app", ex);
        }

        var removed = accessory with { State = AccessoryAuthorizationState.Unauthorized };
        _ = services.RunDelegates<IAccessorySetupDelegate>(x => x.OnAccessoryRemoved(removed), logger);
        return Task.CompletedTask;
    }


    /// <inheritdoc />
    public Task StartObservingPresence(SetupAccessory accessory, CancellationToken cancelToken = default)
    {
        this.ObservePresence(accessory, true);
        return Task.CompletedTask;
    }


    /// <inheritdoc />
    public Task StopObservingPresence(SetupAccessory accessory, CancellationToken cancelToken = default)
    {
        this.ObservePresence(accessory, false);
        return Task.CompletedTask;
    }


    void ObservePresence(SetupAccessory accessory, bool start)
    {
        ArgumentNullException.ThrowIfNull(accessory);
        this.AssertSupported();
        if (!OperatingSystem.IsAndroidVersionAtLeast(31))
            throw new AccessorySetupException(AccessorySetupErrorCode.NotSupported, "Companion device presence needs Android 12 or later");

        try
        {
            if (OperatingSystem.IsAndroidVersionAtLeast(36))
            {
                // identifiers are association ids on every version that gets here
                if (!Int32.TryParse(accessory.Identifier, out var id))
                    throw new AccessorySetupException(AccessorySetupErrorCode.NotAssociated, $"'{accessory.Identifier}' is not an association id");

                var request = new ObservingDevicePresenceRequest.Builder().SetAssociationId(id).Build();
                if (start)
                    this.Cdm.StartObservingDevicePresence(request);
                else
                    this.Cdm.StopObservingDevicePresence(request);
            }
            else
            {
                var address = accessory.MacAddress ?? accessory.Identifier;
#pragma warning disable CA1422 // the address form is the only one before Android 16
                if (start)
                    this.Cdm.StartObservingDevicePresence(address);
                else
                    this.Cdm.StopObservingDevicePresence(address);
#pragma warning restore CA1422
            }
        }
        catch (Java.Lang.SecurityException ex)
        {
            throw new AccessorySetupException(
                AccessorySetupErrorCode.InvalidRequest,
                "Observing presence needs android.permission.REQUEST_OBSERVE_COMPANION_DEVICE_PRESENCE in AndroidManifest.xml",
                ex
            );
        }
        catch (Exception ex) when (ex is DeviceNotAssociatedException or Java.Lang.IllegalArgumentException)
        {
            throw new AccessorySetupException(AccessorySetupErrorCode.NotAssociated, $"Accessory '{accessory.Identifier}' is not associated with this app", ex);
        }
    }


    // called by ShinyCompanionDeviceService
    internal void OnPresence(AssociationInfo? info, string? address, bool isNearby)
    {
        try
        {
            SetupAccessory? accessory = null;
            if (info != null && OperatingSystem.IsAndroidVersionAtLeast(33))
                accessory = ToAccessory(info, null);
            else if (address != null)
                accessory = ToAccessory(address, null);

            if (accessory == null)
                return;

            logger.LogDebug("Companion device {Id} {Presence}", accessory.Identifier, isNearby ? "appeared" : "disappeared");
            _ = services.RunDelegates<IAccessorySetupDelegate>(x => x.OnPresenceChanged(accessory, isNearby), logger);
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error handling companion device presence");
        }
    }


    // called by ShinyCompanionDeviceService on Android 16+
    [SupportedOSPlatform("android36.0")]
    internal void OnPresence(int associationId, bool isNearby)
    {
        var info = this.Cdm.MyAssociations?.FirstOrDefault(x => x?.Id == associationId);
        if (info == null)
        {
            logger.LogWarning("Presence event for unknown companion association {Id}", associationId);
            return;
        }
        this.OnPresence(info, null, isNearby);
    }


    async Task<SetupAccessory?> ReadResult(Intent? data, AssociateCallback callback, string? fallbackName, CancellationToken cancelToken)
    {
        var scan = GetScanResult(data);
        var name = scan?.ScanRecord?.DeviceName ?? scan?.Device?.Name ?? fallbackName;

        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var info = GetAssociationExtra(data);
            if (info == null)
            {
                try
                {
                    info = await callback.Created.Task
                        .WaitAsync(AssociationCallbackTimeout, cancelToken)
                        .ConfigureAwait(false) as AssociationInfo;
                }
                catch (TimeoutException)
                {
                    logger.LogWarning("The companion device dialog returned OK without an association");
                }
            }
            if (info != null)
                return ToAccessory(info, name);
        }

        var address = scan?.Device?.Address;
        return address == null ? null : ToAccessory(address, name);
    }


    void AssertSupported()
    {
        if (!this.IsSupported)
            throw new AccessorySetupException(AccessorySetupErrorCode.NotSupported, "This device does not support companion device setup");
    }


    static IDeviceFilter ToDeviceFilter(AccessoryDiscoveryFilter f)
    {
        var scan = new ScanFilter.Builder();

        if (f.ServiceUuid != null)
        {
            var uuid = ParcelUuid.FromString(AccessoryFilterValidator.ToFullUuid(f.ServiceUuid).ToString())!;
            scan.SetServiceUuid(uuid);

            if (f.ServiceData != null)
                scan.SetServiceData(uuid, f.ServiceData, f.ServiceDataMask);
        }

        if (f.CompanyIdentifier is { } company)
            scan.SetManufacturerData(company, f.ManufacturerData ?? [], f.ManufacturerDataMask);

        var filter = new BluetoothLeDeviceFilter.Builder().SetScanFilter(scan.Build());
        if (f.NameSubstring != null)
            filter.SetNamePattern(Pattern.Compile(".*" + Pattern.Quote(f.NameSubstring) + ".*"));

        return filter.Build();
    }


    static NativeScanResult? GetScanResult(Intent? data)
    {
        if (data == null)
            return null;

        // EXTRA_DEVICE is deprecated on Android 13 in favour of EXTRA_ASSOCIATION, but still carries the scan
        // result - the only place the advertised name is
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
#pragma warning disable CA1422
            return data.GetParcelableExtra(CompanionDeviceManager.ExtraDevice, Java.Lang.Class.FromType(typeof(NativeScanResult))) as NativeScanResult;
#pragma warning restore CA1422
        }

#pragma warning disable CA1422
        return data.GetParcelableExtra(CompanionDeviceManager.ExtraDevice) as NativeScanResult;
#pragma warning restore CA1422
    }


    [SupportedOSPlatform("android33.0")]
    static AssociationInfo? GetAssociationExtra(Intent? data)
        => data?.GetParcelableExtra(CompanionDeviceManager.ExtraAssociation, Java.Lang.Class.FromType(typeof(AssociationInfo))) as AssociationInfo;


    [SupportedOSPlatform("android33.0")]
    static SetupAccessory ToAccessory(AssociationInfo info, string? name)
    {
        var address = info.DeviceMacAddress?.ToString()?.ToUpperInvariant();
        return new SetupAccessory(
            info.Id.ToString(),
            info.DisplayName ?? name,
            address == null ? null : Peripheral.GetUuidFromAddress(address),
            AccessoryAuthorizationState.Authorized
        )
        {
            MacAddress = address
        };
    }


    static SetupAccessory ToAccessory(string address, string? name)
    {
        address = address.ToUpperInvariant();
        return new SetupAccessory(address, name, Peripheral.GetUuidFromAddress(address), AccessoryAuthorizationState.Authorized)
        {
            MacAddress = address
        };
    }


    sealed class AssociateCallback(ILogger logger) : CompanionDeviceManager.Callback
    {
        public TaskCompletionSource<IntentSender?> Pending { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        // AssociationInfo, which only exists from Android 13
        public TaskCompletionSource<Java.Lang.Object> Created { get; } = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public string? FailureReason { get; private set; }


        // Android 13+
        public override void OnAssociationPending(IntentSender intentSender)
            => this.Pending.TrySetResult(intentSender);


        // before Android 13 (and still called by the default OnAssociationPending, which this overrides)
#pragma warning disable CS0672, CA1422
        public override void OnDeviceFound(IntentSender intentSender)
            => this.Pending.TrySetResult(intentSender);
#pragma warning restore CS0672, CA1422


        [SupportedOSPlatform("android33.0")]
        public override void OnAssociationCreated(AssociationInfo associationInfo)
            => this.Created.TrySetResult(associationInfo);


        // Android 16+ says why. A discovery timeout, a cancel or a rejection is "no accessory", like iOS; anything
        // else is a real failure.
        [SupportedOSPlatform("android36.0")]
        public override void OnFailure(int errorCode, Java.Lang.ICharSequence? error)
        {
            var reason = error?.ToString() ?? $"error {errorCode}";
            switch ((CompanionDeviceResult)errorCode)
            {
                case CompanionDeviceResult.DiscoveryTimeout:
                case CompanionDeviceResult.Canceled:
                case CompanionDeviceResult.UserRejected:
                    this.NoDevice(reason);
                    break;

                default:
                    this.Pending.TrySetException(new AccessorySetupException(
                        errorCode == (int)CompanionDeviceResult.SecurityError ? AccessorySetupErrorCode.InvalidRequest : AccessorySetupErrorCode.Failed,
                        $"Companion device association failed: {reason}"
                    ));
                    break;
            }
        }


        // Before Android 16 there is only a message, and in practice it is a discovery timeout or a dismissed dialog
        // - so it reads as "no accessory" and is logged rather than thrown. Once the dialog is up, its activity result
        // is the answer and this is ignored.
        public override void OnFailure(Java.Lang.ICharSequence? error)
        {
            var reason = error?.ToString() ?? "unknown";
            logger.LogInformation("Companion device association ended: {Reason}", reason);
            this.NoDevice(reason);
        }


        void NoDevice(string reason)
        {
            this.FailureReason = reason;
            this.Pending.TrySetResult(null);
        }
    }
}
