using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Content;
using Android.Gms.Location;
using Shiny.Infrastructure;
using Shiny.Logging;


namespace Shiny.Locations
{
    public class GeofenceProcessor
    {
        readonly IEnumerable<IGeofenceDelegate> delegates;
        readonly RepositoryWrapper<GeofenceRegion, GeofenceRegionStore> repository;


        public GeofenceProcessor(IRepository repository, IEnumerable<IGeofenceDelegate> delegates)
        {
            this.repository = repository.Wrap();
            this.delegates = delegates;
        }


        public async Task Process(Intent intent)
        {
            var e = GeofencingEvent.FromIntent(intent);
            if (e == null)
                return;

            
            if (e.HasError)
            {
                Log.Write(
                    LocationLogCategory.Geofence,
                    "Event Error",
                    ("ErrorCode", GeofenceStatusCodes.GetStatusCodeString(e.ErrorCode))
                );
            }
            else if (e.TriggeringGeofences != null)
            {
                foreach (var triggeringGeofence in e.TriggeringGeofences)
                {
                    var state = (GeofenceState)e.GeofenceTransition;
                    var region = await this.repository.Get(triggeringGeofence.RequestId);

                    if (region == null)
                    {
                        Log.Write(
                            LocationLogCategory.Geofence,
                            "Not Found",
                            ("RequestId", triggeringGeofence.RequestId)
                        );
                    }
                    else
                    {
                        await this.delegates.RunDelegates(
                            x => x.OnStatusChanged(state, region),
                            ex => Log.Write(
                                ex,
                                ("RequestId", triggeringGeofence.RequestId),
                                ("Transition", state.ToString())
                            )
                        );
                    }
                }
            }
        }
    }
}
