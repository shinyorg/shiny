using System;
using Android.App;
using Android.Content;
using static Android.Manifest;


namespace Shiny.Beacons
{
    [BroadcastReceiver(
        Name = "com.shiny.beacons.BeaconBroadcastReceiver",
        Exported = true,
        DirectBootAware = true
    )]
    [IntentFilter(new [] {
        Permission.ReceiveBootCompleted
    })]
    public class BeaconBroadcastReceiver : BroadcastReceiver
    {
        public override void OnReceive(Context context, Intent intent)
            => context.StartService(new Intent(context, typeof(BeaconService)));
    }
}
