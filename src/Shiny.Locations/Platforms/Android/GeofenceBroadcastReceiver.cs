using System;
using Android.App;
using Android.Content;
using static Android.Manifest;


namespace Shiny.Locations
{
    [BroadcastReceiver(
        Name = "com.shiny.locations.GeofenceBroadcastReceiver",
        Exported = true,
        DirectBootAware = true
    )]
    [IntentFilter(new [] {
        "com.shiny.locations.GeofenceBroadcastReceiver.ACTION_PROCESS",
        Permission.ReceiveBootCompleted
    })]
    public class GeofenceBroadcastReceiver : BroadcastReceiver
    {
        public const string INTENT_ACTION = "com.shiny.locations.GeofenceBroadcastReceiver.ACTION_PROCESS";


        public override void OnReceive(Context context, Intent intent) => this.Execute(async () =>
        {
            // TODO: this is no longer registered
            var geofences = ShinyHost.Resolve<IGeofenceManager>() as IAndroidGeofenceManager;
            if (geofences == null)
                throw new ArgumentException("Invalid AndroidGeofenceManager");

            switch (intent.Action)
            {
                case INTENT_ACTION:
                    await geofences.Process(intent);
                    break;

                case Permission.ReceiveBootCompleted:
                    await geofences.ReceiveBoot();
                    break;
            }
        });
    }
}
