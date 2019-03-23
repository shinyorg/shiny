using System;
using Shiny.Infrastructure;
using Shiny.Logging;
using CoreLocation;


namespace Shiny.Locations
{
    public class GeofenceManagerDelegate : CLLocationManagerDelegate
    {
        public override void RegionEntered(CLLocationManager manager, CLRegion region)
            => this.Broadcast(region, GeofenceState.Entered);


        public override void RegionLeft(CLLocationManager manager, CLRegion region)
            => this.Broadcast(region, GeofenceState.Exited);


        public event EventHandler<CLAuthorizationStatus> AuthStatusChanged;
        public override void AuthorizationChanged(CLLocationManager manager, CLAuthorizationStatus status)
            => this.AuthStatusChanged?.Invoke(this, status);


        public event EventHandler<CLRegionStateDeterminedEventArgs> DeterminedState;
        public override void DidDetermineState(CLLocationManager manager, CLRegionState state, CLRegion region)
            => this.DeterminedState?.Invoke(this, new CLRegionStateDeterminedEventArgs(state, region));


        async void Broadcast(CLRegion region, GeofenceState status)
        {
            try
            {
                if (region is CLCircularRegion native)
                {
                    var repo = ShinyHost.Resolve<IRepository>().Wrap();
                    var geofence = await repo.Get(native.Identifier);

                    if (geofence != null)
                    {
                        ShinyHost.Resolve<IGeofenceDelegate>().OnStatusChanged(status, geofence);

                        if (geofence.SingleUse)
                            await ShinyHost.Resolve<IGeofenceManager>().StopMonitoring(geofence);
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }
    }
}
