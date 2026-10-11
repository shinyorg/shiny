using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using Shiny.Hosting;
using P = Android.Manifest.Permission;

namespace Shiny;


public static partial class ShinyPermissions
{
    static string[] BleCentralPermissions => OperatingSystem.IsAndroidVersionAtLeast(31)
        ? [P.BluetoothScan, P.BluetoothConnect]
        : [P.Bluetooth, P.BluetoothAdmin, P.AccessFineLocation];


    static async Task<AccessState> RequestThenAdapter(string[] permissions)
    {
        var state = await ShinyPermission.RequestPermissions(permissions).ConfigureAwait(false);
        return state == AccessState.Available ? ShinyPermission.GetAdapterState() : state;
    }


    static AccessState StatusThenAdapter(string[] permissions)
    {
        var state = ShinyPermission.GetPermissionStatus(permissions);
        return state == AccessState.Available ? ShinyPermission.GetAdapterState() : state;
    }


    static async Task<AccessState> RequestNotifications()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var state = await ShinyPermission.RequestPermissions(P.PostNotifications).ConfigureAwait(false);
            if (state != AccessState.Available)
                return state;
        }
        return ShinyPermission.AreNotificationsEnabled() ? AccessState.Available : AccessState.Disabled;
    }


    static AccessState GetNotificationStatus()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var state = ShinyPermission.GetPermissionStatus(P.PostNotifications);
            if (state != AccessState.Available)
                return state;
        }
        return ShinyPermission.AreNotificationsEnabled() ? AccessState.Available : AccessState.Disabled;
    }


    static string[] NotificationPermissions => OperatingSystem.IsAndroidVersionAtLeast(33) ? [P.PostNotifications] : [];


    /// <summary>
    /// Both granted is available, one granted is restricted - the read/write pairs for contacts and calendars.
    /// </summary>
    static AccessState PairStatus(string read, string write)
    {
        var platform = ShinyPermission.Platform;
        var r = platform.GetCurrentPermissionStatus(read);
        var w = platform.GetCurrentPermissionStatus(write);
        if (r == AccessState.Available && w == AccessState.Available)
            return AccessState.Available;

        if (r == AccessState.Available || w == AccessState.Available)
            return AccessState.Restricted;

        return r == AccessState.Denied || w == AccessState.Denied ? AccessState.Denied : AccessState.Unknown;
    }


    static async Task<AccessState> RequestPair(string read, string write)
    {
        await ShinyPermission.Platform.RequestPermissions(read, write).ConfigureAwait(false);
        var state = PairStatus(read, write);
        return state == AccessState.Unknown ? AccessState.Denied : state;
    }


    public sealed partial class BluetoothLE
    {
        protected override string[] AndroidPermissions => BleCentralPermissions;
        protected override Task<AccessState> GetAccessState() => Task.FromResult(StatusThenAdapter(BleCentralPermissions));
        protected override Task<AccessState> RequestAccessState() => RequestThenAdapter(BleCentralPermissions);
    }


    public sealed partial class BluetoothLEHosting
    {
        static string[] Permissions => OperatingSystem.IsAndroidVersionAtLeast(31)
            ? [P.BluetoothAdvertise, P.BluetoothConnect]
            : [];

        protected override string[] AndroidPermissions => Permissions;
        protected override Task<AccessState> GetAccessState() => Task.FromResult(StatusThenAdapter(Permissions));
        protected override Task<AccessState> RequestAccessState() => RequestThenAdapter(Permissions);
    }


    public sealed partial class Beacons
    {
        protected override string[] AndroidPermissions => BleCentralPermissions;
        protected override Task<AccessState> GetAccessState() => Task.FromResult(StatusThenAdapter(BleCentralPermissions));
        protected override Task<AccessState> RequestAccessState() => RequestThenAdapter(BleCentralPermissions);
    }


    public sealed partial class BeaconMonitoring
    {
        static string[] Permissions
        {
            get
            {
                var list = new List<string>(BleCentralPermissions);
                if (OperatingSystem.IsAndroidVersionAtLeast(34))
                    list.Add(P.ForegroundServiceConnectedDevice);

                if (OperatingSystem.IsAndroidVersionAtLeast(33))
                    list.Add(P.PostNotifications);

                return list.ToArray();
            }
        }

        protected override string[] AndroidPermissions => Permissions;
        protected override Task<AccessState> GetAccessState() => Task.FromResult(StatusThenAdapter(Permissions));
        protected override Task<AccessState> RequestAccessState() => RequestThenAdapter(Permissions);
    }


    public sealed partial class Gps
    {
        protected override string[] AndroidPermissions => ShinyPermission.LocationPermissions(false);
        protected override Task<AccessState> GetAccessState() => Task.FromResult(ShinyPermission.GetLocationStatus(false));
        protected override Task<AccessState> RequestAccessState() => ShinyPermission.RequestLocation(false);
    }


    public sealed partial class GpsBackground
    {
        protected override string[] AndroidPermissions => ShinyPermission.LocationPermissions(true);
        protected override Task<AccessState> GetAccessState() => Task.FromResult(ShinyPermission.GetLocationStatus(true));
        protected override Task<AccessState> RequestAccessState() => ShinyPermission.RequestLocation(true);
    }


    public sealed partial class GpsRealtime
    {
        // matches Shiny.Gps: the foreground service stands in for background location from Android 12 on,
        // and Android 10/11 still ask for background location
        static bool NeedsBackground => OperatingSystem.IsAndroidVersionAtLeast(29) && !OperatingSystem.IsAndroidVersionAtLeast(31);

        static string[] Extras
        {
            get
            {
                var list = new List<string>();
                if (OperatingSystem.IsAndroidVersionAtLeast(31))
                    list.Add(P.ForegroundService);

                if (OperatingSystem.IsAndroidVersionAtLeast(33))
                    list.Add(P.PostNotifications);

                if (OperatingSystem.IsAndroidVersionAtLeast(34))
                    list.Add(P.ForegroundServiceLocation);

                return list.ToArray();
            }
        }

        protected override string[] AndroidPermissions => ShinyPermission.LocationPermissions(NeedsBackground, Extras);
        protected override Task<AccessState> GetAccessState() => Task.FromResult(ShinyPermission.GetLocationStatus(NeedsBackground, Extras));
        protected override Task<AccessState> RequestAccessState() => ShinyPermission.RequestLocation(NeedsBackground, Extras);
    }


    public sealed partial class Geofencing
    {
        protected override string[] AndroidPermissions => ShinyPermission.LocationPermissions(true);
        protected override Task<AccessState> GetAccessState() => Task.FromResult(ShinyPermission.GetLocationStatus(true));
        protected override Task<AccessState> RequestAccessState() => ShinyPermission.RequestLocation(true);
    }


    public sealed partial class MotionActivity
    {
        static string[] Permissions => OperatingSystem.IsAndroidVersionAtLeast(29) ? [P.ActivityRecognition] : [];

        protected override string[] AndroidPermissions => Permissions;
        protected override Task<AccessState> GetAccessState() => Task.FromResult(
            Permissions.Length == 0 ? AccessState.Available : ShinyPermission.GetPermissionStatus(Permissions)
        );
        protected override Task<AccessState> RequestAccessState() => ShinyPermission.RequestPermissions(Permissions);
    }


    public sealed partial class Notifications
    {
        protected override string[] AndroidPermissions => NotificationPermissions;
        protected override Task<AccessState> GetAccessState() => Task.FromResult(GetNotificationStatus());
        protected override Task<AccessState> RequestAccessState() => RequestNotifications();
    }


    public sealed partial class TimeSensitiveNotifications
    {
        static bool CanScheduleExactAlarms()
        {
            if (!OperatingSystem.IsAndroidVersionAtLeast(31))
                return true;

            var alarms = (AlarmManager?)ShinyPermission.Platform.AppContext.GetSystemService(Context.AlarmService);
            return alarms?.CanScheduleExactAlarms() ?? false;
        }


        protected override string[] AndroidPermissions => NotificationPermissions;


        protected override Task<AccessState> GetAccessState()
        {
            var state = GetNotificationStatus();
            if (state == AccessState.Available && !CanScheduleExactAlarms())
                state = AccessState.Restricted;

            return Task.FromResult(state);
        }


        protected override async Task<AccessState> RequestAccessState()
        {
            var state = await RequestNotifications().ConfigureAwait(false);
            if (state != AccessState.Available || !OperatingSystem.IsAndroidVersionAtLeast(31) || CanScheduleExactAlarms())
                return state;

            // SCHEDULE_EXACT_ALARM is an app-op granted from Settings, not a runtime permission
            var platform = ShinyPermission.Platform;
            var intent = new Intent(
                Android.Provider.Settings.ActionRequestScheduleExactAlarm,
                Android.Net.Uri.Parse("package:" + platform.AppContext.PackageName)
            );
            intent.AddFlags(ActivityFlags.NewTask);
            platform.AppContext.StartActivity(intent);

            await platform.WaitForActivity(ActivityState.Resumed).ConfigureAwait(false);
            return CanScheduleExactAlarms() ? AccessState.Available : AccessState.Restricted;
        }
    }


    public sealed partial class Push
    {
        protected override string[] AndroidPermissions => NotificationPermissions;
        protected override Task<AccessState> GetAccessState() => Task.FromResult(GetNotificationStatus());
        protected override Task<AccessState> RequestAccessState() => RequestNotifications();
    }


    public sealed partial class HttpTransfers
    {
        protected override string[] AndroidPermissions => ShinyPermission.ForegroundServicePermissions;
        protected override Task<AccessState> GetAccessState() => Task.FromResult(ShinyPermission.GetForegroundServiceStatus());
        protected override Task<AccessState> RequestAccessState() => ShinyPermission.Platform.RequestForegroundServicePermissions();
    }


    public sealed partial class DataSync
    {
        protected override string[] AndroidPermissions => ShinyPermission.ForegroundServicePermissions;
        protected override Task<AccessState> GetAccessState() => Task.FromResult(ShinyPermission.GetForegroundServiceStatus());
        protected override Task<AccessState> RequestAccessState() => ShinyPermission.Platform.RequestForegroundServicePermissions();
    }


    public sealed partial class ScreenRecorder
    {
        protected override string[] AndroidPermissions => ShinyPermission.ForegroundServicePermissions;
        protected override Task<AccessState> GetAccessState() => Task.FromResult(ShinyPermission.GetForegroundServiceStatus());
        protected override Task<AccessState> RequestAccessState() => ShinyPermission.Platform.RequestForegroundServicePermissions();
    }


    public sealed partial class Jobs
    {
        // WorkManager needs no runtime permission
        protected override Task<AccessState> GetAccessState() => Task.FromResult(AccessState.Available);
    }


    public sealed partial class Wifi
    {
        static string[] Permissions => OperatingSystem.IsAndroidVersionAtLeast(33)
            ? [P.AccessFineLocation, P.NearbyWifiDevices]
            : [P.AccessFineLocation];


        static AccessState Combine(AccessState fine, AccessState nearby)
        {
            if (fine != AccessState.Available)
                return fine;

            return nearby == AccessState.Available ? AccessState.Available : AccessState.Restricted;
        }


        protected override string[] AndroidPermissions => Permissions;


        protected override Task<AccessState> GetAccessState()
        {
            var platform = ShinyPermission.Platform;
            var fine = platform.GetCurrentPermissionStatus(P.AccessFineLocation);
            var nearby = OperatingSystem.IsAndroidVersionAtLeast(33)
                ? platform.GetCurrentPermissionStatus(P.NearbyWifiDevices)
                : AccessState.Available;

            return Task.FromResult(Combine(fine, nearby));
        }


        protected override async Task<AccessState> RequestAccessState()
        {
            var result = await ShinyPermission.Platform.RequestPermissions(Permissions).ConfigureAwait(false);
            var fine = result.IsGranted(P.AccessFineLocation) ? AccessState.Available : AccessState.Denied;
            var nearby = !OperatingSystem.IsAndroidVersionAtLeast(33) || result.IsGranted(P.NearbyWifiDevices)
                ? AccessState.Available
                : AccessState.Denied;

            return Combine(fine, nearby);
        }
    }


    public sealed partial class Contacts
    {
        protected override string[] AndroidPermissions => [P.ReadContacts, P.WriteContacts];
        protected override Task<AccessState> GetAccessState() => Task.FromResult(PairStatus(P.ReadContacts, P.WriteContacts));
        protected override Task<AccessState> RequestAccessState() => RequestPair(P.ReadContacts, P.WriteContacts);
    }


    public sealed partial class Calendar
    {
        protected override string[] AndroidPermissions => [P.ReadCalendar, P.WriteCalendar];
        protected override Task<AccessState> GetAccessState() => Task.FromResult(PairStatus(P.ReadCalendar, P.WriteCalendar));
        protected override Task<AccessState> RequestAccessState() => RequestPair(P.ReadCalendar, P.WriteCalendar);
    }
}
