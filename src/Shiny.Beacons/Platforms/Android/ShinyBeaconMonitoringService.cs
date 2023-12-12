using Android.App;
using Android.Content;
using Android.Content.PM;

namespace Shiny.Beacons;


[Service(
    Enabled = true,
    Exported = true,
    ForegroundServiceType = ForegroundService.TypeLocation
)]
public class ShinyBeaconMonitoringService : ShinyAndroidForegroundService<IBeaconMonitoringManager, IBeaconMonitorDelegate>
{
    public static bool IsStarted { get; private set; }
    BackgroundTask? backgroundTask;
    protected override ForegroundService StartForegroundServiceType => ForegroundService.TypeLocation;


    protected override void OnStart(Intent? intent)
    {
        this.backgroundTask = this.GetService<BackgroundTask>();
        this.backgroundTask.Run();
        IsStarted = true;
    }


    protected override void OnStop()
    {
        this.backgroundTask?.StopScan();
        IsStarted = false;
    }
}