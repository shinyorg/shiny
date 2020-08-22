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
            // this broadcastreceiver being registered will cause application to spinup shiny infrastructure
            // thereby starting the foreground service if beacons are registered
        }
    }
}
