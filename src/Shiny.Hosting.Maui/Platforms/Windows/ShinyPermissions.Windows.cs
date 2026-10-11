using System;
using System.Threading.Tasks;
using Windows.ApplicationModel.Appointments;
using Windows.Devices.Bluetooth;
using Windows.Devices.Geolocation;
using Windows.Devices.Radios;
using Windows.Devices.WiFi;
using Windows.Graphics.Capture;
using Windows.UI.Notifications;

namespace Shiny;


public static partial class ShinyPermissions
{
    /// <summary>
    /// There is no Bluetooth consent on Windows - what matters is an LE-capable adapter whose radio is on.
    /// </summary>
    static async Task<AccessState> GetRadioState(bool peripheralRole)
    {
        var adapter = await BluetoothAdapter.GetDefaultAsync();
        if (adapter == null || !adapter.IsLowEnergySupported || (peripheralRole && !adapter.IsPeripheralRoleSupported))
            return AccessState.NotSupported;

        var radio = await adapter.GetRadioAsync();
        return radio?.State switch
        {
            RadioState.On => AccessState.Available,
            RadioState.Off => AccessState.Disabled,
            RadioState.Disabled => AccessState.Disabled,
            null => AccessState.NotSupported,
            _ => AccessState.Unknown
        };
    }


    static async Task<AccessState> RequestLocation()
    {
        var access = await Geolocator.RequestAccessAsync();
        return access switch
        {
            GeolocationAccessStatus.Allowed => AccessState.Available,
            GeolocationAccessStatus.Denied => GetLocationState() == AccessState.Disabled ? AccessState.Disabled : AccessState.Denied,
            _ => AccessState.Unknown
        };
    }


    static AccessState GetLocationState() => ShinyPermission.GetCapabilityState("location");


    static AccessState GetToastState()
    {
        try
        {
            return ToastNotificationManager.CreateToastNotifier().Setting switch
            {
                NotificationSetting.Enabled => AccessState.Available,
                NotificationSetting.DisabledByManifest => AccessState.NotSupported,
                NotificationSetting.DisabledByGroupPolicy => AccessState.Disabled,
                _ => AccessState.Denied // turned off for the app or for the user
            };
        }
        catch
        {
            // an unpackaged app has no toast notifier until it registers an AUMID - there is nothing to consent to
            return AccessState.Available;
        }
    }


    public sealed partial class BluetoothLE
    {
        protected override Task<AccessState> GetAccessState() => GetRadioState(false);
    }


    public sealed partial class BluetoothLEHosting
    {
        protected override Task<AccessState> GetAccessState() => GetRadioState(true);
    }


    public sealed partial class Beacons
    {
        protected override Task<AccessState> GetAccessState() => GetRadioState(false);
    }


    public sealed partial class BeaconMonitoring
    {
        protected override Task<AccessState> GetAccessState() => GetRadioState(false);
    }


    public sealed partial class Gps
    {
        protected override string[] WindowsCapabilities => ["location"];
        protected override Task<AccessState> GetAccessState() => Task.FromResult(GetLocationState());
        protected override Task<AccessState> RequestAccessState() => RequestLocation();
    }


    public sealed partial class Geofencing
    {
        protected override string[] WindowsCapabilities => ["location"];
        protected override Task<AccessState> GetAccessState() => Task.FromResult(GetLocationState());
        protected override Task<AccessState> RequestAccessState() => RequestLocation();
    }


    public sealed partial class Notifications
    {
        protected override Task<AccessState> GetAccessState() => Task.FromResult(GetToastState());
    }


    public sealed partial class TimeSensitiveNotifications
    {
        protected override Task<AccessState> GetAccessState() => Task.FromResult(GetToastState());
    }


    public sealed partial class Push
    {
        // WNS channels need no consent - showing what arrives is the toast setting
        protected override Task<AccessState> GetAccessState() => Task.FromResult(GetToastState());
    }


    public sealed partial class HttpTransfers
    {
        protected override Task<AccessState> GetAccessState() => Task.FromResult(AccessState.Available);
    }


    public sealed partial class DataSync
    {
        protected override Task<AccessState> GetAccessState() => Task.FromResult(AccessState.Available);
    }


    public sealed partial class Wifi
    {
        protected override string[] WindowsCapabilities => ["wiFiControl"];
        protected override Task<AccessState> GetAccessState() => Task.FromResult(ShinyPermission.GetCapabilityState("wiFiControl"));


        protected override async Task<AccessState> RequestAccessState()
        {
            var access = await WiFiAdapter.RequestAccessAsync();
            return access switch
            {
                WiFiAccessStatus.Allowed => AccessState.Available,
                WiFiAccessStatus.DeniedByUser => AccessState.Denied,
                WiFiAccessStatus.DeniedBySystem => AccessState.Disabled,
                _ => AccessState.Unknown
            };
        }
    }


    public sealed partial class Calendar
    {
        protected override string[] WindowsCapabilities => ["appointments"];
        protected override Task<AccessState> GetAccessState() => Task.FromResult(ShinyPermission.GetCapabilityState("appointments"));


        protected override async Task<AccessState> RequestAccessState()
        {
            try
            {
                // the read/write store a third-party app can have - Shiny.Calendar's ReadWrite
                var store = await AppointmentManager.RequestStoreAsync(AppointmentStoreAccessType.AppCalendarsReadWrite);
                return store == null ? AccessState.Denied : AccessState.Available;
            }
            catch (UnauthorizedAccessException)
            {
                return AccessState.Denied;
            }
        }
    }


    public sealed partial class ScreenRecorder
    {
        protected override Task<AccessState> GetAccessState() => Task.FromResult(
            GraphicsCaptureSession.IsSupported() ? AccessState.Available : AccessState.NotSupported
        );
    }
}
