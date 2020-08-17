using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;


namespace Shiny.Beacons
{
    [Service(
        Enabled = false,
        ForegroundServiceType = ForegroundService.TypeLocation
    )]
    public class ShinyBeaconMonitoringService : Service
    {
        Lazy<BackgroundTask> task = ShinyHost.LazyResolve<BackgroundTask>();

        // TODO: persistent notification?
        [return: GeneratedEnum]
        public override StartCommandResult OnStartCommand(Intent? intent, [GeneratedEnum] StartCommandFlags flags, int startId)
        {
            this.task.Value.StartScan();
            return StartCommandResult.NotSticky;
        }


        public override void OnDestroy()
        {
            this.task.Value.StopScan();
            base.OnDestroy();
        }


        public override IBinder? OnBind(Intent? intent) => null;
    }
}
