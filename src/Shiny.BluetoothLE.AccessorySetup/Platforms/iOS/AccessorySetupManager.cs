using AccessorySetupKit;
using CoreBluetooth;
using CoreFoundation;
using System.Runtime.Versioning;
using Foundation;
using Microsoft.Extensions.Logging;
using Shiny.BluetoothLE.AccessorySetup.Infrastructure;
using UIKit;

namespace Shiny.BluetoothLE.AccessorySetup;


/// <summary>
/// <see cref="IAccessorySetupManager"/> over AccessorySetupKit (iOS 18+). The session is activated at startup, not on
/// first use, so removals and renames the user makes in Settings while the app is not running reach
/// <see cref="IAccessorySetupDelegate"/> on the next launch.
/// <para>
/// Shiny.BluetoothLE creates its <c>CBCentralManager</c> lazily, so an app that only reaches its accessories through
/// AccessorySetupKit never shows the general Bluetooth prompt - as long as it leaves <c>NSBluetoothAlwaysUsageDescription</c>
/// out of Info.plist and does not scan.
/// </para>
/// </summary>
public class AccessorySetupManager(
    IServiceProvider services,
    ILogger<AccessorySetupManager> logger
) : IAccessorySetupManager, IShinyStartupTask
{
    const string FallbackSymbol = "dot.radiowaves.left.and.right";

    readonly TaskCompletionSource activation = new(TaskCreationOptions.RunContinuationsAsynchronously);
    readonly object sync = new();
    ASAccessorySession? session;
    PickerFlow? picker;


    /// <inheritdoc />
    [SupportedOSPlatformGuard("ios18.0")]
    public bool IsSupported => OperatingSystem.IsIOSVersionAtLeast(18);

    /// <inheritdoc />
    public bool IsPresenceSupported => false;


    /// <inheritdoc />
    public void Start()
    {
        if (!OperatingSystem.IsIOSVersionAtLeast(18))
        {
            logger.LogInformation("AccessorySetupKit needs iOS 18 or later");
            this.activation.TrySetResult();
            return;
        }

        this.session = new ASAccessorySession();
        this.session.Activate(DispatchQueue.MainQueue, this.OnEvent);
    }


    /// <inheritdoc />
    public async Task<IReadOnlyList<SetupAccessory>> GetAccessories(CancellationToken cancelToken = default)
    {
        if (!this.IsSupported)
            return [];

        var s = await this.GetActiveSession(cancelToken).ConfigureAwait(false);
        return (s.Accessories ?? []).Select(ToAccessory).ToList();
    }


    /// <inheritdoc />
    public async Task<SetupAccessory?> ShowPicker(
        IReadOnlyList<AccessoryPickerItem> items,
        AccessoryPickerOptions? options = null,
        CancellationToken cancelToken = default
    )
    {
        AccessoryFilterValidator.Validate(items);
        if (!this.IsSupported)
            throw NotSupported();

        var s = await this.GetActiveSession(cancelToken).ConfigureAwait(false);

        var flow = new PickerFlow();
        lock (this.sync)
        {
            if (this.picker is { IsCompleted: false })
                throw new AccessorySetupException(AccessorySetupErrorCode.PickerAlreadyActive, "The accessory picker is already showing");

            this.picker = flow;
        }

        using var reg = cancelToken.Register(() => flow.OnCancelToken(cancelToken));
        try
        {
            await InvokeOnMainThread(() =>
            {
                if (OperatingSystem.IsIOSVersionAtLeast(18))
                    Present(s, items, options, flow);
            }).ConfigureAwait(false);

            return await flow.Task.ConfigureAwait(false);
        }
        finally
        {
            lock (this.sync)
            {
                if (this.picker == flow)
                    this.picker = null;
            }
        }
    }


    [SupportedOSPlatform("ios18.0")]
    static void Present(ASAccessorySession s, IReadOnlyList<AccessoryPickerItem> items, AccessoryPickerOptions? options, PickerFlow flow)
    {
        if (options?.DiscoveryTimeout is { } timeout && OperatingSystem.IsIOSVersionAtLeast(26))
            s.PickerDisplaySettings = new ASPickerDisplaySettings { DiscoveryTimeout = timeout.TotalSeconds };

        var displayItems = items.Select(ToDisplayItem).ToArray();
        s.ShowPicker(displayItems, error =>
        {
            if (error == null)
                flow.OnShowCompleted(null);
            else if (IsCancellation(error))
                flow.OnCancelled();
            else
                flow.OnShowCompleted(ToException(error, "The accessory picker could not be shown"));
        });
    }


    /// <inheritdoc />
    public async Task Remove(SetupAccessory accessory, CancellationToken cancelToken = default)
    {
        ArgumentNullException.ThrowIfNull(accessory);
        if (!this.IsSupported)
            throw NotSupported();

        var s = await this.GetActiveSession(cancelToken).ConfigureAwait(false);
        ASAccessory? native = null;
        foreach (var candidate in s.Accessories ?? [])
        {
            if (GetIdentifier(candidate) == accessory.Identifier)
            {
                native = candidate;
                break;
            }
        }
        if (native == null)
            throw new AccessorySetupException(AccessorySetupErrorCode.NotAssociated, $"Accessory '{accessory.Identifier}' is not granted to this app");

        try
        {
            await s.RemoveAccessoryAsync(native).WaitAsync(cancelToken).ConfigureAwait(false);
        }
        catch (NSErrorException ex)
        {
            throw ToException(ex.Error, "The accessory could not be removed");
        }
    }


    /// <inheritdoc />
    public Task StartObservingPresence(SetupAccessory accessory, CancellationToken cancelToken = default)
        => throw new AccessorySetupException(AccessorySetupErrorCode.NotSupported, "Accessory presence is Android only - iOS has no equivalent for AccessorySetupKit accessories");


    /// <inheritdoc />
    public Task StopObservingPresence(SetupAccessory accessory, CancellationToken cancelToken = default)
        => this.StartObservingPresence(accessory, cancelToken);


    [SupportedOSPlatform("ios18.0")]
    void OnEvent(ASAccessoryEvent ev)
    {
        try
        {
            logger.LogDebug("AccessorySetupKit event {Event}", ev.EventType);
            switch (ev.EventType)
            {
                case ASAccessoryEventType.Activated:
                    this.activation.TrySetResult();
                    break;

                case ASAccessoryEventType.Invalidated:
                    this.activation.TrySetException(ToException(ev.Error, "The accessory session could not be activated"));
                    this.CurrentPicker()?.OnInvalidated();
                    break;

                case ASAccessoryEventType.AccessoryAdded when ev.Accessory != null:
                    var added = ToAccessory(ev.Accessory);
                    this.CurrentPicker()?.OnAccessoryAdded(added);
                    this.RunDelegates(x => x.OnAccessoryAdded(added));
                    break;

                case ASAccessoryEventType.AccessoryRemoved when ev.Accessory != null:
                    var removed = ToAccessory(ev.Accessory) with { State = AccessoryAuthorizationState.Unauthorized };
                    this.RunDelegates(x => x.OnAccessoryRemoved(removed));
                    break;

                case ASAccessoryEventType.AccessoryChanged when ev.Accessory != null:
                    var changed = ToAccessory(ev.Accessory);
                    this.RunDelegates(x => x.OnAccessoryChanged(changed));
                    break;

                case ASAccessoryEventType.PickerSetupFailed:
                    this.CurrentPicker()?.OnSetupFailed(ToException(ev.Error, "Accessory setup failed"));
                    break;

                case ASAccessoryEventType.PickerDidDismiss:
                    this.CurrentPicker()?.OnDismissed();
                    break;
            }
        }
        catch (Exception ex)
        {
            // never let an exception escape into the native event callback
            logger.LogError(ex, "Error handling AccessorySetupKit event {Event}", ev.EventType);
        }
    }


    PickerFlow? CurrentPicker()
    {
        lock (this.sync)
            return this.picker;
    }


    void RunDelegates(Func<IAccessorySetupDelegate, Task> action)
        => _ = services.RunDelegates(action, logger);


    async Task<ASAccessorySession> GetActiveSession(CancellationToken cancelToken)
    {
        await this.activation.Task.WaitAsync(cancelToken).ConfigureAwait(false);
        return this.session
            ?? throw new AccessorySetupException(AccessorySetupErrorCode.Failed, "The accessory session is not active - is the Shiny host started?");
    }


    static AccessorySetupException NotSupported()
        => new(AccessorySetupErrorCode.NotSupported, "AccessorySetupKit needs iOS 18 or later");


    [SupportedOSPlatform("ios18.0")]
    static ASPickerDisplayItem ToDisplayItem(AccessoryPickerItem item)
    {
        var f = item.Filter;
        var descriptor = new ASDiscoveryDescriptor
        {
            BluetoothRange = f.Range == AccessoryRange.Immediate
                ? ASDiscoveryDescriptorRange.Immediate
                : ASDiscoveryDescriptorRange.Default
        };

        if (f.ServiceUuid != null)
            descriptor.BluetoothServiceUuid = CBUUID.FromString(f.ServiceUuid);

        if (f.ServiceData != null)
        {
            descriptor.BluetoothServiceDataBlob = NSData.FromArray(f.ServiceData);
            descriptor.BluetoothServiceDataMask = NSData.FromArray(f.ServiceDataMask ?? FullMask(f.ServiceData.Length));
        }

        if (f.CompanyIdentifier is { } company)
            descriptor.BluetoothCompanyIdentifier = company;

        if (f.ManufacturerData != null)
        {
            descriptor.BluetoothManufacturerDataBlob = NSData.FromArray(f.ManufacturerData);
            descriptor.BluetoothManufacturerDataMask = NSData.FromArray(f.ManufacturerDataMask ?? FullMask(f.ManufacturerData.Length));
        }

        if (f.NameSubstring != null)
            descriptor.BluetoothNameSubstring = f.NameSubstring;

        if (f.RequiresPairing)
            descriptor.SupportedOptions = ASAccessorySupportOptions.BluetoothPairingLE;

        var displayItem = new ASPickerDisplayItem(item.Name, LoadImage(item.ProductImage), descriptor);
        if (item.AllowRename)
            displayItem.SetupOptions = ASPickerDisplayItemSetupOptions.Rename;

        return displayItem;
    }


    static UIImage LoadImage(string? image)
    {
        UIImage? result = null;
        if (!String.IsNullOrWhiteSpace(image))
        {
            result = Path.IsPathRooted(image) && File.Exists(image)
                ? UIImage.FromFile(image)
                : UIImage.FromBundle(image);
        }
        return result ?? UIImage.GetSystemImage(FallbackSymbol) ?? new UIImage();
    }


    static byte[] FullMask(int length) => Enumerable.Repeat((byte)0xFF, length).ToArray();


    [SupportedOSPlatform("ios18.0")]
    static string GetIdentifier(ASAccessory accessory)
        => accessory.BluetoothIdentifier?.ToString()
        ?? accessory.Ssid
        ?? accessory.DisplayName;


    [SupportedOSPlatform("ios18.0")]
    static SetupAccessory ToAccessory(ASAccessory accessory) => new(
        GetIdentifier(accessory),
        accessory.DisplayName,
        accessory.BluetoothIdentifier?.ToString(),
        accessory.State switch
        {
            ASAccessoryState.Authorized => AccessoryAuthorizationState.Authorized,
            ASAccessoryState.AwaitingAuthorization => AccessoryAuthorizationState.AwaitingAuthorization,
            _ => AccessoryAuthorizationState.Unauthorized
        }
    );


    [SupportedOSPlatform("ios18.0")]
    static bool IsCancellation(NSError error)
        => IsAccessorySetupError(error)
        && (ASErrorCode)(long)error.Code is ASErrorCode.UserCancelled or ASErrorCode.DiscoveryTimeout;


    [SupportedOSPlatform("ios18.0")]
    static bool IsAccessorySetupError(NSError error)
        => error.Domain == ASErrorCode.Success.GetDomain()?.ToString();


    [SupportedOSPlatform("ios18.0")]
    static AccessorySetupException ToException(NSError? error, string message)
    {
        if (error == null)
            return new AccessorySetupException(AccessorySetupErrorCode.Failed, message);

        var code = IsAccessorySetupError(error)
            ? (ASErrorCode)(long)error.Code switch
            {
                ASErrorCode.PickerAlreadyActive => AccessorySetupErrorCode.PickerAlreadyActive,
                ASErrorCode.PickerRestricted or ASErrorCode.UserRestricted => AccessorySetupErrorCode.Restricted,
                ASErrorCode.InvalidRequest => AccessorySetupErrorCode.InvalidRequest,
                _ => AccessorySetupErrorCode.Failed
            }
            : AccessorySetupErrorCode.Failed;

        var detail = code == AccessorySetupErrorCode.InvalidRequest
            ? " - check that Info.plist declares NSAccessorySetupSupports (Bluetooth) and every service UUID, company identifier and name the filters use (NSAccessorySetupBluetoothServices / CompanyIdentifiers / Names)"
            : String.Empty;

        return new AccessorySetupException(code, $"{message}: {error.LocalizedDescription}{detail}", new NSErrorException(error));
    }


    static Task InvokeOnMainThread(Action action)
    {
        if (NSThread.IsMain)
        {
            action();
            return Task.CompletedTask;
        }

        var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
        DispatchQueue.MainQueue.DispatchAsync(() =>
        {
            try
            {
                action();
                tcs.TrySetResult();
            }
            catch (Exception ex)
            {
                tcs.TrySetException(ex);
            }
        });
        return tcs.Task;
    }
}
