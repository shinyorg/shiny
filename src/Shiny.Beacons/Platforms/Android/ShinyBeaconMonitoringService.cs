using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;

namespace Shiny.Beacons;


/// <summary>
/// Keeps the beacon monitoring scan running while the app is backgrounded.
/// </summary>
/// <remarks>
/// The service type is <see cref="ForegroundService.TypeConnectedDevice"/>, not
/// <c>TypeLocation</c>. Android 14 enforces that a foreground service's declared type matches what
/// it actually does, and a BLE scan is a connected-device activity - declaring it as location gets
/// the service refused on API 34+, and asks the user for a permission this never uses.
/// </remarks>
// Fully qualified: Shiny.Extensions.DependencyInjection also ships a [Service] attribute, and an
// unqualified one here binds to that instead.
[Android.App.Service(
    Enabled = true,
    Exported = false,
    ForegroundServiceType = ForegroundService.TypeConnectedDevice
)]
public class ShinyBeaconMonitoringService : ShinyAndroidForegroundService<IBeaconMonitoringManager, IBeaconMonitorDelegate>
{
    static volatile bool isStarted;

    /// <summary>Whether the service is currently running.</summary>
    public static bool IsStarted => isStarted;

    /// <inheritdoc />
    protected override ForegroundService StartForegroundServiceType => ForegroundService.TypeConnectedDevice;


    /// <inheritdoc />
    protected override void OnStart(Intent? intent)
    {
        // A null intent means Android restarted the service after killing the process. The manager
        // reloads its regions from the repository in its own constructor, so simply resolving it
        // through the base class is enough to re-arm monitoring.
        if (this.Service is AndroidBeaconMonitoringManager manager)
            manager.OnServiceStarted();

        isStarted = true;
    }


    /// <inheritdoc />
    protected override void OnStop()
    {
        if (this.Service is AndroidBeaconMonitoringManager manager)
            manager.OnServiceStopped();

        isStarted = false;
    }


    /// <inheritdoc />
    public override IBinder? OnBind(Intent? intent) => null;
}
