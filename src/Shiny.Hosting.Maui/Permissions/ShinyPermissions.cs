namespace Shiny;


/// <summary>
/// MAUI-compatible permission checks for the Shiny modules. Use them like any MAUI permission:
/// <code>
/// var status = await Permissions.RequestAsync&lt;ShinyPermissions.BluetoothLE&gt;();
/// </code>
/// They ask the operating system directly, so none of them needs the matching Shiny module referenced. Each one
/// mirrors what that module's own <c>RequestAccess</c> asks for. See <see cref="ShinyPermission"/> for how the
/// results map to <see cref="Microsoft.Maui.ApplicationModel.PermissionStatus"/>.
/// </summary>
public static partial class ShinyPermissions
{
    /// <summary>
    /// Scanning for and connecting to BLE peripherals (Shiny.BluetoothLE). Android: <c>BLUETOOTH_SCAN</c> +
    /// <c>BLUETOOTH_CONNECT</c> on 12+, <c>BLUETOOTH</c>/<c>BLUETOOTH_ADMIN</c>/<c>ACCESS_FINE_LOCATION</c> below, then the
    /// adapter state. Apple: CoreBluetooth central authorization and power state. Windows: the Bluetooth radio.
    /// </summary>
    public sealed partial class BluetoothLE : ShinyPermission;

    /// <summary>
    /// Advertising and hosting a GATT server (Shiny.BluetoothLE.Hosting). Android: <c>BLUETOOTH_ADVERTISE</c> +
    /// <c>BLUETOOTH_CONNECT</c> on 12+, then the adapter state. Apple: CoreBluetooth peripheral authorization and power
    /// state. Windows: whether the adapter supports the peripheral role.
    /// </summary>
    public sealed partial class BluetoothLEHosting : ShinyPermission;

    /// <summary>
    /// Beacon ranging (Shiny.Beacons). Android and Windows: the same as <see cref="BluetoothLE"/>. Apple: when-in-use
    /// location, which CoreLocation ranging requires.
    /// </summary>
    public sealed partial class Beacons : ShinyPermission;

    /// <summary>
    /// Background beacon monitoring (Shiny.Beacons). Android: <see cref="BluetoothLE"/> plus the connected-device
    /// foreground service and <c>POST_NOTIFICATIONS</c>. iOS / Mac Catalyst: "always" location. macOS has no beacon
    /// region monitoring.
    /// </summary>
    public sealed partial class BeaconMonitoring : ShinyPermission;

    /// <summary>
    /// Foreground GPS (Shiny.Gps). Android: coarse + fine location. Apple: when-in-use location. Windows: location.
    /// Approximate location only reports <see cref="Microsoft.Maui.ApplicationModel.PermissionStatus.Limited"/>.
    /// </summary>
    public sealed partial class Gps : ShinyPermission;

    /// <summary>
    /// Background GPS - <c>GpsBackgroundMode.Standard</c> (Shiny.Gps). Android: coarse + fine, then background location
    /// as its own step on 10+. Apple: "always" location, asked for after when-in-use. Not supported on Windows.
    /// </summary>
    public sealed partial class GpsBackground : ShinyPermission;

    /// <summary>
    /// Realtime background GPS - <c>GpsBackgroundMode.Realtime</c> (Shiny.Gps). Android: coarse + fine plus the location
    /// foreground service and <c>POST_NOTIFICATIONS</c>. Apple: "always" location. Not supported on Windows.
    /// </summary>
    public sealed partial class GpsRealtime : ShinyPermission;

    /// <summary>
    /// Geofence monitoring (Shiny.Geofencing). Android: coarse + fine, then background location on 10+. iOS / Mac
    /// Catalyst: "always" location. Windows: location.
    /// </summary>
    public sealed partial class Geofencing : ShinyPermission;

    /// <summary>
    /// Motion activity recognition (Shiny.Gps). Android: <c>ACTIVITY_RECOGNITION</c> on 10+. iOS / Mac Catalyst:
    /// CoreMotion activity authorization.
    /// </summary>
    public sealed partial class MotionActivity : ShinyPermission;

    /// <summary>
    /// Local notifications (Shiny.Notifications). Android: <c>POST_NOTIFICATIONS</c> on 13+ and notifications enabled
    /// for the app. Apple: alert, badge and sound authorization. Windows: the app's toast setting.
    /// </summary>
    public sealed partial class Notifications : ShinyPermission;

    /// <summary>
    /// Notifications that must fire on time (<c>AccessRequestFlags.TimeSensitivity</c> in Shiny.Notifications).
    /// Everything <see cref="Notifications"/> checks, plus exact alarms on Android 12+ - requesting opens the
    /// system setting and waits for the user to come back.
    /// </summary>
    public sealed partial class TimeSensitiveNotifications : ShinyPermission;

    /// <summary>
    /// The user-facing part of push notifications (Shiny.Push). Android: <c>POST_NOTIFICATIONS</c> on 13+. Apple: alert,
    /// badge and sound authorization. Windows needs no consent. Registering for a token is still
    /// done by the push manager.
    /// </summary>
    public sealed partial class Push : ShinyPermission;

    /// <summary>
    /// Background HTTP transfers (Shiny.Net.Http). Android: the foreground service and <c>POST_NOTIFICATIONS</c> for its
    /// notification - notifications denied reports <see cref="Microsoft.Maui.ApplicationModel.PermissionStatus.Limited"/>,
    /// because transfers still run. Apple and Windows need no consent.
    /// </summary>
    public sealed partial class HttpTransfers : ShinyPermission;

    /// <summary>
    /// Background data sync (Shiny.Data.Sync). The same as <see cref="HttpTransfers"/>.
    /// </summary>
    public sealed partial class DataSync : ShinyPermission;

    /// <summary>
    /// Background jobs (Shiny.Jobs). iOS / Mac Catalyst: the <c>processing</c> background mode and
    /// <c>BGTaskSchedulerPermittedIdentifiers</c> must be declared, and Background App Refresh must be on. Android and
    /// macOS need no consent.
    /// </summary>
    public sealed partial class Jobs : ShinyPermission;

    /// <summary>
    /// Wi-Fi scanning and connecting (Shiny.Net.Wifi). Android: fine location, plus <c>NEARBY_WIFI_DEVICES</c> on 13+.
    /// Apple: when-in-use location, which reading the current network requires. Windows: Wi-Fi adapter access.
    /// </summary>
    public sealed partial class Wifi : ShinyPermission;

    /// <summary>
    /// Reading and writing contacts (Shiny.Contacts). Android: <c>READ_CONTACTS</c> + <c>WRITE_CONTACTS</c>. Apple: the
    /// Contacts store - a limited selection on iOS 18 reports
    /// <see cref="Microsoft.Maui.ApplicationModel.PermissionStatus.Limited"/>.
    /// </summary>
    public sealed partial class Contacts : ShinyPermission;

    /// <summary>
    /// Full calendar access (Shiny.Calendar). Android: <c>READ_CALENDAR</c> + <c>WRITE_CALENDAR</c>. Apple: full access to
    /// events (iOS 17 / macOS 14 and up), or event access before that. Windows: the appointment store.
    /// </summary>
    public sealed partial class Calendar : ShinyPermission;

    /// <summary>
    /// Screen recording (Shiny.ScreenRecorder), without audio - pair it with MAUI's <c>Permissions.Microphone</c> to
    /// record the microphone. Android: the foreground service and its notification; the capture consent itself is
    /// asked for on every recording. iOS / Mac Catalyst: whether ReplayKit is available. macOS: screen capture access,
    /// which only takes effect after the app relaunches. Windows: whether screen capture is supported.
    /// </summary>
    public sealed partial class ScreenRecorder : ShinyPermission;
}
