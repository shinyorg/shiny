using System;

using Android.App;
using Android.Content;


namespace Shiny.Beacons
{
    [BroadcastReceiver(
        Enabled = true,
        Exported = false
    )]
    [IntentFilter(new[] {
        Intent.ActionBootCompleted
    })]
    public class ShinyBeaconMonitoringBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            ShinyHost
                .Resolve<AndroidContext>()
                .StartService(
                    typeof(ShinyBeaconMonitoringService),
                    true
                );
        }
    }
}
