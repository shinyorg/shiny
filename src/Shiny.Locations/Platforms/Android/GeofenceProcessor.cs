using System;
using System.Threading.Tasks;
using Android.Content;
using Android.Gms.Location;
using Shiny.Infrastructure;


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

            foreach (var triggeringGeofence in e.TriggeringGeofences)
            {
                var region = await this.repository.Get(triggeringGeofence.RequestId);
                if (region != null)
                {
                    var state = (GeofenceState)e.GeofenceTransition;
                    await this.geofenceDelegate.OnStatusChanged(state, region);

                    if (region.SingleUse)
                        await this.geofenceManager.StopMonitoring(region);
                }
            }
        }
    }
}
