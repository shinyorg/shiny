using System.Runtime.Versioning;
using Android.App;
using Android.Companion;

namespace Shiny.BluetoothLE.AccessorySetup;


/// <summary>
/// How the system tells the app an observed companion device came into or left range - it binds this service,
/// starting the app first if it is not running. Everything goes to <see cref="AccessorySetupManager"/>, which the Shiny
/// host has started by the time a service is created.
/// <para>
/// The service only receives events for accessories passed to
/// <see cref="IAccessorySetupManager.StartObservingPresence"/>, and the app needs
/// <c>android.permission.REQUEST_OBSERVE_COMPANION_DEVICE_PRESENCE</c> in its manifest.
/// </para>
/// </summary>
[Android.App.Service(
    Exported = true,
    Permission = "android.permission.BIND_COMPANION_DEVICE_SERVICE"
)]
[IntentFilter(["android.companion.CompanionDeviceService"])]
[SupportedOSPlatform("android31.0")]
public class ShinyCompanionDeviceService : CompanionDeviceService
{
    static AccessorySetupManager? Manager => AccessorySetupManager.Current;


    // Android 16+ reports everything through presence events; the per-callback overloads below are not called there
    public override void OnDevicePresenceEvent(DevicePresenceEvent e)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(36))
            return;

        bool? nearby = e.Event switch
        {
            DevicePresenceEventType.BleAppeared or
            DevicePresenceEventType.BtConnected or
            DevicePresenceEventType.SelfManagedAppeared => true,

            DevicePresenceEventType.BleDisappeared or
            DevicePresenceEventType.BtDisconnected or
            DevicePresenceEventType.SelfManagedDisappeared => false,

            _ => null
        };

        if (nearby != null)
            Manager?.OnPresence(e.AssociationId, nearby.Value);
    }


#pragma warning disable CS0672, CA1422 // the per-callback forms are what Android 12 - 15 call
    // Android 13 - 15
    public override void OnDeviceAppeared(AssociationInfo associationInfo)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(36))
            Manager?.OnPresence(associationInfo, null, true);
    }


    public override void OnDeviceDisappeared(AssociationInfo associationInfo)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(36))
            Manager?.OnPresence(associationInfo, null, false);
    }


    // Android 12
    public override void OnDeviceAppeared(string address)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            Manager?.OnPresence(null, address, true);
    }


    public override void OnDeviceDisappeared(string address)
    {
        if (!OperatingSystem.IsAndroidVersionAtLeast(33))
            Manager?.OnPresence(null, address, false);
    }
#pragma warning restore CS0672, CA1422
}
