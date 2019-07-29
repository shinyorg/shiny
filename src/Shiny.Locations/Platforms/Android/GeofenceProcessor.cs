using System;
using System.Threading.Tasks;
using Android.Content;
using Android.Gms.Location;
using Shiny.Infrastructure;
using Shiny.Logging;


namespace Shiny.Locations
{
    public class GeofenceProcessor
    {
        readonly IGeofenceManager geofenceManager;
        readonly IGeofenceDelegate geofenceDelegate;
        readonly RepositoryWrapper<GeofenceRegion, GeofenceRegionStore> repository;


        public GeofenceProcessor(IRepository repository,
                                 IGeofenceManager geofenceManager,
                                 IGeofenceDelegate geofenceDelegate)
        {
            this.repository = repository.Wrap();
            this.geofenceManager = geofenceManager;
            this.geofenceDelegate = geofenceDelegate;
        }


        public async Task Process(Intent intent)
        {
            var e = GeofencingEvent.FromIntent(intent);
            if (e == null)
                return;

            if (e.HasError)
            {
                Log.Write(LocationLogCategory.Geofence, "Event Error",
                    ("ErrorCode", GeofenceStatusCodes.GetStatusCodeString(e.ErrorCode)));
                return;
            }

            foreach (var triggeringGeofence in e.TriggeringGeofences)
            {
                var state = (GeofenceState)e.GeofenceTransition;

                var region = await this.repository.Get(triggeringGeofence.RequestId);
                if (region == null)
                {
                    Log.Write(LocationLogCategory.Geofence, "Not Found",
                        ("RequestId", triggeringGeofence.RequestId));
                    continue;
                }

                try
                {
                    await this.geofenceDelegate.OnStatusChanged(state, region);

                    if (region.SingleUse)
                        await this.geofenceManager.StopMonitoring(region);
                }
                catch (Exception ex)
                {
                    Log.Write(ex,
                        ("RequestId", triggeringGeofence.RequestId),
                        ("Transition", state.ToString()));
                }
            }
        }
    }
}
