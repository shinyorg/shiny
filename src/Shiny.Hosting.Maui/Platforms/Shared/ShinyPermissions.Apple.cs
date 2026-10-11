#if APPLE
using System;
using System.Linq;
using System.Threading.Tasks;
using Contacts;
using CoreBluetooth;
using CoreFoundation;
using CoreLocation;
using EventKit;
using Foundation;
using Microsoft.Maui.ApplicationModel;
using UserNotifications;
using MauiPermissionException = Microsoft.Maui.ApplicationModel.PermissionException;
#if !MACOS
using CoreMotion;
using ReplayKit;
using UIKit;
#endif

namespace Shiny;


public static partial class ShinyPermissions
{
    const string BluetoothKey = "NSBluetoothAlwaysUsageDescription";
    static readonly TimeSpan BluetoothStateTimeout = TimeSpan.FromSeconds(10);


    static PermissionStatus? FromBluetoothAuthorization(CBManagerAuthorization authorization) => authorization switch
    {
        CBManagerAuthorization.NotDetermined => PermissionStatus.Unknown,
        CBManagerAuthorization.Denied => PermissionStatus.Denied,
        CBManagerAuthorization.Restricted => PermissionStatus.Restricted,
        _ => null // allowed - the power state decides
    };


    static PermissionStatus FromBluetoothState(CBManagerState state, ShinyPermission permission) => state switch
    {
        CBManagerState.PoweredOn => PermissionStatus.Granted,
        CBManagerState.Resetting => PermissionStatus.Granted,
        CBManagerState.PoweredOff => PermissionStatus.Disabled,
        CBManagerState.Unauthorized => PermissionStatus.Denied,
        CBManagerState.Unsupported => throw new FeatureNotSupportedException($"{permission.GetType().Name} is not supported on this device"),
        _ => PermissionStatus.Unknown
    };


    /// <summary>
    /// Creating a central manager is what prompts, and the state it settles on says both whether access was granted
    /// and whether the radio is on.
    /// </summary>
    static Task<PermissionStatus> GetCentralState(ShinyPermission permission) => ShinyPermission.OnMainThread(async () =>
    {
        var del = new CentralDelegate();
        using var manager = new CBCentralManager(del, DispatchQueue.MainQueue, new CBCentralInitOptions { ShowPowerAlert = false });
        var state = await WaitForState(del.State, manager.State);
        manager.Delegate = null!;
        return FromBluetoothState(state, permission);
    });


    static Task<PermissionStatus> GetPeripheralState(ShinyPermission permission) => ShinyPermission.OnMainThread(async () =>
    {
        var del = new PeripheralDelegate();
        using var manager = new CBPeripheralManager(del, DispatchQueue.MainQueue);
        var state = await WaitForState(del.State, manager.State);
        manager.Delegate = null!;
        return FromBluetoothState(state, permission);
    });


    static async Task<CBManagerState> WaitForState(Task<CBManagerState> updated, CBManagerState current)
    {
        if (current != CBManagerState.Unknown)
            return current;

        var first = await Task.WhenAny(updated, Task.Delay(BluetoothStateTimeout));
        return first == updated ? updated.Result : CBManagerState.Unknown;
    }


    sealed class CentralDelegate : CBCentralManagerDelegate
    {
        readonly TaskCompletionSource<CBManagerState> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<CBManagerState> State => this.tcs.Task;

        public override void UpdatedState(CBCentralManager central)
        {
            if (central.State != CBManagerState.Unknown)
                this.tcs.TrySetResult(central.State);
        }
    }


    sealed class PeripheralDelegate : CBPeripheralManagerDelegate
    {
        readonly TaskCompletionSource<CBManagerState> tcs = new(TaskCreationOptions.RunContinuationsAsynchronously);
        public Task<CBManagerState> State => this.tcs.Task;

        public override void StateUpdated(CBPeripheralManager peripheral)
        {
            if (peripheral.State != CBManagerState.Unknown)
                this.tcs.TrySetResult(peripheral.State);
        }
    }


    static async Task<PermissionStatus> GetBleCentralStatus(ShinyPermission permission)
        => FromBluetoothAuthorization(CBManager.Authorization) ?? await GetCentralState(permission).ConfigureAwait(false);


    static async Task<PermissionStatus> GetBlePeripheralStatus(ShinyPermission permission)
        => FromBluetoothAuthorization(CBManager.Authorization) ?? await GetPeripheralState(permission).ConfigureAwait(false);


    // notifications
    static PermissionStatus FromNotificationSettings(UNNotificationSettings settings) => settings.AuthorizationStatus switch
    {
        UNAuthorizationStatus.NotDetermined => PermissionStatus.Unknown,
        UNAuthorizationStatus.Denied => PermissionStatus.Denied,
        _ => PermissionStatus.Granted // authorized, provisional and ephemeral all deliver
    };


    static async Task<PermissionStatus> GetNotificationStatus()
    {
        var settings = await UNUserNotificationCenter.Current.GetNotificationSettingsAsync().ConfigureAwait(false);
        return FromNotificationSettings(settings);
    }


    static async Task<PermissionStatus> RequestNotifications()
    {
        var center = UNUserNotificationCenter.Current;
        var settings = await center.GetNotificationSettingsAsync().ConfigureAwait(false);
        if (settings.AuthorizationStatus == UNAuthorizationStatus.NotDetermined)
        {
            await center
                .RequestAuthorizationAsync(UNAuthorizationOptions.Alert | UNAuthorizationOptions.Badge | UNAuthorizationOptions.Sound)
                .ConfigureAwait(false);

            settings = await center.GetNotificationSettingsAsync().ConfigureAwait(false);
        }
        return FromNotificationSettings(settings);
    }


    public sealed partial class BluetoothLE
    {
        protected override void EnsureInfoPlist() => RequireInfoPlistKey(BluetoothKey);
        protected override Task<PermissionStatus> GetStatus() => GetBleCentralStatus(this);
        protected override Task<PermissionStatus> Request() => GetCentralState(this);
    }


    public sealed partial class BluetoothLEHosting
    {
        protected override void EnsureInfoPlist() => RequireInfoPlistKey(BluetoothKey);
        protected override Task<PermissionStatus> GetStatus() => GetBlePeripheralStatus(this);
        protected override Task<PermissionStatus> Request() => GetPeripheralState(this);
    }


    public sealed partial class Beacons
    {
        void AssertRanging()
        {
#if !MACOS
            if (!CLLocationManager.IsRangingAvailable)
                throw this.NotSupported();
#endif
        }

        protected override void EnsureInfoPlist() => AppleLocationPermission.EnsureInfoPlist(false);

        protected override Task<PermissionStatus> GetStatus()
        {
            this.AssertRanging();
            return AppleLocationPermission.GetStatus(false);
        }

        protected override Task<PermissionStatus> Request()
        {
            this.AssertRanging();
            return AppleLocationPermission.Request(false);
        }
    }


    public sealed partial class BeaconMonitoring
    {
        void AssertMonitoring()
        {
#if MACOS
            // CoreLocation on the Mac has no beacon region monitoring
            throw this.NotSupported();
#else
            if (!CLLocationManager.IsMonitoringAvailable(typeof(CLBeaconRegion)))
                throw this.NotSupported();
#endif
        }

        protected override void EnsureInfoPlist() => AppleLocationPermission.EnsureInfoPlist(true);

        protected override Task<PermissionStatus> GetStatus()
        {
            this.AssertMonitoring();
            return AppleLocationPermission.GetStatus(true);
        }

        protected override Task<PermissionStatus> Request()
        {
            this.AssertMonitoring();
            return AppleLocationPermission.Request(true);
        }
    }


    public sealed partial class Gps
    {
        protected override void EnsureInfoPlist() => AppleLocationPermission.EnsureInfoPlist(false);
        protected override Task<PermissionStatus> GetStatus() => AppleLocationPermission.GetStatus(false);
        protected override Task<PermissionStatus> Request() => AppleLocationPermission.Request(false);
    }


    public sealed partial class GpsBackground
    {
        protected override void EnsureInfoPlist() => AppleLocationPermission.EnsureInfoPlist(true);
        protected override Task<PermissionStatus> GetStatus() => AppleLocationPermission.GetStatus(true);
        protected override Task<PermissionStatus> Request() => AppleLocationPermission.Request(true);
    }


    public sealed partial class GpsRealtime
    {
        protected override void EnsureInfoPlist() => AppleLocationPermission.EnsureInfoPlist(true);
        protected override Task<PermissionStatus> GetStatus() => AppleLocationPermission.GetStatus(true);
        protected override Task<PermissionStatus> Request() => AppleLocationPermission.Request(true);
    }


    public sealed partial class Geofencing
    {
        void AssertMonitoring()
        {
#if !MACOS
            if (!CLLocationManager.IsMonitoringAvailable(typeof(CLCircularRegion)))
                throw this.NotSupported();
#endif
        }

        protected override void EnsureInfoPlist() => AppleLocationPermission.EnsureInfoPlist(true);

        protected override Task<PermissionStatus> GetStatus()
        {
            this.AssertMonitoring();
            return AppleLocationPermission.GetStatus(true);
        }

        protected override Task<PermissionStatus> Request()
        {
            this.AssertMonitoring();
            return AppleLocationPermission.Request(true);
        }
    }


    public sealed partial class MotionActivity
    {
#if MACOS
        // CoreMotion has no activity recognition on the Mac
        protected override Task<PermissionStatus> GetStatus() => throw this.NotSupported();
#else
        void AssertAvailable()
        {
            if (!CMMotionActivityManager.IsActivityAvailable)
                throw this.NotSupported();
        }


        static PermissionStatus FromNative(CMAuthorizationStatus status) => status switch
        {
            CMAuthorizationStatus.Authorized => PermissionStatus.Granted,
            CMAuthorizationStatus.Denied => PermissionStatus.Denied,
            CMAuthorizationStatus.Restricted => PermissionStatus.Restricted,
            _ => PermissionStatus.Unknown
        };


        protected override void EnsureInfoPlist() => RequireInfoPlistKey("NSMotionUsageDescription");


        protected override Task<PermissionStatus> GetStatus()
        {
            this.AssertAvailable();
            return Task.FromResult(FromNative(CMMotionActivityManager.AuthorizationStatus));
        }


        protected override async Task<PermissionStatus> Request()
        {
            this.AssertAvailable();
            if (CMMotionActivityManager.AuthorizationStatus == CMAuthorizationStatus.NotDetermined)
            {
                // there is no request API - the first activity query is what prompts
                using var manager = new CMMotionActivityManager();
                var tcs = new TaskCompletionSource(TaskCreationOptions.RunContinuationsAsynchronously);
                manager.QueryActivity(
                    NSDate.FromTimeIntervalSinceNow(-1),
                    NSDate.Now,
                    NSOperationQueue.MainQueue,
                    (_, _) => tcs.TrySetResult()
                );
                await tcs.Task.ConfigureAwait(false);
            }
            return FromNative(CMMotionActivityManager.AuthorizationStatus);
        }
#endif
    }


    public sealed partial class Notifications
    {
        protected override Task<PermissionStatus> GetStatus() => GetNotificationStatus();
        protected override Task<PermissionStatus> Request() => RequestNotifications();
    }


    public sealed partial class TimeSensitiveNotifications
    {
        // time sensitive delivery is an entitlement on Apple, not something the user is asked for
        protected override Task<PermissionStatus> GetStatus() => GetNotificationStatus();
        protected override Task<PermissionStatus> Request() => RequestNotifications();
    }


    public sealed partial class Push
    {
        protected override Task<PermissionStatus> GetStatus() => GetNotificationStatus();
        protected override Task<PermissionStatus> Request() => RequestNotifications();
    }


    public sealed partial class HttpTransfers
    {
        // background NSURLSession transfers need no consent
        protected override Task<AccessState> GetAccessState() => Task.FromResult(AccessState.Available);
    }


    public sealed partial class DataSync
    {
        protected override Task<AccessState> GetAccessState() => Task.FromResult(AccessState.Available);
    }


    public sealed partial class Jobs
    {
#if MACOS
        protected override Task<AccessState> GetAccessState() => Task.FromResult(AccessState.Available);
#else
        protected override void EnsureInfoPlist()
        {
            var modes = NSBundle.MainBundle.ObjectForInfoDictionary("UIBackgroundModes") as NSArray;
            var hasProcessing = modes != null && Enumerable
                .Range(0, (int)modes.Count)
                .Any(i => modes.GetItem<NSString>((nuint)i)?.ToString() == "processing");

            if (!hasProcessing)
                throw new MauiPermissionException("You must add `processing` to UIBackgroundModes in your Info.plist");

            RequireInfoPlistKey("BGTaskSchedulerPermittedIdentifiers");
        }


        protected override Task<PermissionStatus> GetStatus() => ShinyPermission.OnMainThread(() =>
            UIApplication.SharedApplication.BackgroundRefreshStatus switch
            {
                UIBackgroundRefreshStatus.Available => PermissionStatus.Granted,
                UIBackgroundRefreshStatus.Denied => PermissionStatus.Denied,
                UIBackgroundRefreshStatus.Restricted => PermissionStatus.Restricted,
                _ => PermissionStatus.Unknown
            }
        );


        // the user switches Background App Refresh in Settings - there is no prompt
        protected override Task<PermissionStatus> Request() => this.GetStatus();
#endif
    }


    public sealed partial class Wifi
    {
        protected override void EnsureInfoPlist() => AppleLocationPermission.EnsureInfoPlist(false);
        protected override Task<PermissionStatus> GetStatus() => AppleLocationPermission.GetStatus(false);
        protected override Task<PermissionStatus> Request() => AppleLocationPermission.Request(false);
    }


    public sealed partial class Contacts
    {
        static PermissionStatus Current() => CNContactStore.GetAuthorizationStatus(CNEntityType.Contacts) switch
        {
            CNAuthorizationStatus.Authorized => PermissionStatus.Granted,
            CNAuthorizationStatus.Denied => PermissionStatus.Denied,
            CNAuthorizationStatus.Restricted => PermissionStatus.Restricted,
            CNAuthorizationStatus.NotDetermined => PermissionStatus.Unknown,
            _ => PermissionStatus.Limited // iOS 18 - the user picked which contacts the app sees
        };


        protected override void EnsureInfoPlist() => RequireInfoPlistKey("NSContactsUsageDescription");
        protected override Task<PermissionStatus> GetStatus() => Task.FromResult(Current());


        protected override async Task<PermissionStatus> Request()
        {
            if (CNContactStore.GetAuthorizationStatus(CNEntityType.Contacts) == CNAuthorizationStatus.NotDetermined)
            {
                using var store = new CNContactStore();
                await store.RequestAccessAsync(CNEntityType.Contacts).ConfigureAwait(false);
            }
            return Current();
        }
    }


    public sealed partial class Calendar
    {
        static bool UseFullAccess =>
            OperatingSystem.IsIOSVersionAtLeast(17) ||
            OperatingSystem.IsMacCatalystVersionAtLeast(17) ||
            OperatingSystem.IsMacOSVersionAtLeast(14);


        static PermissionStatus Current() => EKEventStore.GetAuthorizationStatus(EKEntityType.Event) switch
        {
            EKAuthorizationStatus.NotDetermined => PermissionStatus.Unknown,
            EKAuthorizationStatus.Denied => PermissionStatus.Denied,
            EKAuthorizationStatus.Restricted => PermissionStatus.Restricted,
            EKAuthorizationStatus.WriteOnly => PermissionStatus.Limited,
            _ => PermissionStatus.Granted // full access (Authorized before iOS 17)
        };


        protected override void EnsureInfoPlist()
        {
            if (UseFullAccess)
                RequireInfoPlistKey("NSCalendarsFullAccessUsageDescription");
            else
                RequireInfoPlistKey("NSCalendarsUsageDescription");
        }


        protected override Task<PermissionStatus> GetStatus() => Task.FromResult(Current());


        protected override Task<PermissionStatus> Request() => ShinyPermission.OnMainThread(async () =>
        {
            var status = EKEventStore.GetAuthorizationStatus(EKEntityType.Event);
            var upgrade = status == EKAuthorizationStatus.WriteOnly && UseFullAccess;
            if (status == EKAuthorizationStatus.NotDetermined || upgrade)
            {
                using var store = new EKEventStore();
                if (OperatingSystem.IsIOSVersionAtLeast(17) || OperatingSystem.IsMacCatalystVersionAtLeast(17) || OperatingSystem.IsMacOSVersionAtLeast(14))
                {
                    await store.RequestFullAccessToEventsAsync();
                }
                else
                {
#pragma warning disable CA1422 // the only request below iOS 17 / macOS 14
                    await store.RequestAccessAsync(EKEntityType.Event);
#pragma warning restore CA1422
                }
            }
            return Current();
        });
    }


    public sealed partial class ScreenRecorder
    {
#if MACOS
        protected override void EnsureInfoPlist() { }


        protected override Task<PermissionStatus> GetStatus()
        {
            if (!OperatingSystem.IsMacOSVersionAtLeast(12, 3))
                throw this.NotSupported();

            // macOS cannot tell "never asked" from "denied" without asking
            return Task.FromResult(MacScreenCapture.Preflight() ? PermissionStatus.Granted : PermissionStatus.Unknown);
        }


        protected override Task<PermissionStatus> Request() => ShinyPermission.OnMainThread(() =>
        {
            if (!OperatingSystem.IsMacOSVersionAtLeast(12, 3))
                throw this.NotSupported();

            if (MacScreenCapture.Preflight())
                return PermissionStatus.Granted;

            // a grant made from this prompt only takes effect once the app is relaunched
            return MacScreenCapture.Request() ? PermissionStatus.Granted : PermissionStatus.Denied;
        });
#else
        protected override Task<PermissionStatus> GetStatus() => Task.FromResult(
            RPScreenRecorder.SharedRecorder.Available ? PermissionStatus.Granted : PermissionStatus.Disabled
        );


        // ReplayKit asks for consent itself when each recording starts
        protected override Task<PermissionStatus> Request() => this.GetStatus();
#endif
    }
}
#endif
