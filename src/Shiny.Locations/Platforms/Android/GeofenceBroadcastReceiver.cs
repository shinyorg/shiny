using System;
using Shiny.Infrastructure;
using Android.App;
using Android.Content;
using Android.Gms.Location;


namespace Shiny.Locations
{
    [BroadcastReceiver(
        Name = "com.shiny.locations.GeofenceBroadcastReceiver",
        Exported = true,
        DirectBootAware = true
    )]
    [IntentFilter(new [] {
        "com.shiny.locations.GeofenceBroadcastReceiver.ACTION_PROCESS"
    })]
    public class GeofenceBroadcastReceiver : BroadcastReceiver
    {
        public const string INTENT_ACTION = "com.shiny.locations.GeofenceBroadcastReceiver.ACTION_PROCESS";


        public override async void OnReceive(Context context, Intent intent)
        {
            if (!intent.Action.Equals(INTENT_ACTION))
                return;

            var e = GeofencingEvent.FromIntent(intent);
            if (e == null)
                return;

            var repository = ShinyHost.Resolve<IRepository>();
            var geofences = ShinyHost.Resolve<IGeofenceManager>();
            var geofenceDelegate = ShinyHost.Resolve<IGeofenceDelegate>();

            foreach (var triggeringGeofence in e.TriggeringGeofences)
            {
                var region = await repository.Get<GeofenceRegion>(triggeringGeofence.RequestId);
                if (region != null)
                {
                    var state = (GeofenceState)e.GeofenceTransition;
                    geofenceDelegate.OnStatusChanged(state, region);

                    if (region.SingleUse)
                        await geofences.StopMonitoring(region);
                }
            }
        }
    }
}
