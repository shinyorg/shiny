using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Content;
using Android.Gms.Location;
using Microsoft.Extensions.Logging;
using Shiny.Infrastructure;
using Shiny.Locations.Infrastructure;


namespace Shiny.Locations
{
    public class GeofenceProcessor
    {
        readonly IEnumerable<IGeofenceDelegate> delegates;
        readonly RepositoryWrapper<GeofenceRegion, GeofenceRegionStore> repository;
        readonly ILogger logger;


        public GeofenceProcessor(IRepository repository,
                                 ILogger<IGeofenceDelegate> logger,
                                 IEnumerable<IGeofenceDelegate> delegates)
        {
            this.repository = repository.Wrap();
            this.logger = logger;
            this.delegates = delegates;
        }


        public async Task Process(Intent intent)
        {
            var e = GeofencingEvent.FromIntent(intent);
            if (e == null)
                return;

            if (e.HasError)
            {
                var err = GeofenceStatusCodes.GetStatusCodeString(e.ErrorCode);
                this.logger.LogWarning("Geofence OS error - " + err);
            }
            else if (e.TriggeringGeofences != null)
            {
                foreach (var triggeringGeofence in e.TriggeringGeofences)
                {
                    var state = (GeofenceState)e.GeofenceTransition;
                    var region = await this.repository.Get(triggeringGeofence.RequestId);

                    if (region == null)
                    {
                        this.logger.LogWarning("Geofence reported by OS not found in Shiny Repository - RequestID: " + triggeringGeofence.RequestId);
                    }
                    else
                    {
                        await this.delegates.RunDelegates(
                            x => x.OnStatusChanged(state, region),
                            ex => this.logger.LogError($"Error in geofence delegate - Region: {region.Identifier} State: {state}")
                        );
                    }
                }
            }
        }
    }
}
