using System;
using Android.App;
using Android.Content;
using static Android.Manifest;


namespace Shiny.Locations
{
    [BroadcastReceiver(
        Name = "com.shiny.locations.GeofenceBroadcastReceiver",
        Exported = true
    )]
    [IntentFilter(new [] {
        "com.shiny.locations.GeofenceBroadcastReceiver.ACTION_PROCESS",
        Permission.ReceiveBootCompleted
    })]
    public class GeofenceBroadcastReceiver : BroadcastReceiver
    {
        // startup tasks replace this, but this receiver is still used to trigger the wakeup on reboot
        public const string INTENT_ACTION = "com.shiny.locations.GeofenceBroadcastReceiver.ACTION_PROCESS";


        public override void OnReceive(Context context, Intent intent) => this.Execute(async () =>
        {
            if (intent.Action != INTENT_ACTION)
                return;

            var geofences = ShinyHost.Resolve<IGeofenceManager>() as IAndroidGeofenceManager;
            if (geofences == null)
                throw new ArgumentException("Invalid AndroidGeofenceManager");

            await geofences.Process(intent);
        });
    }
}
