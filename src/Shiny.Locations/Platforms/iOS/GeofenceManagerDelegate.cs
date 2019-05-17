using System;
using Shiny.Infrastructure;
using Shiny.Logging;
using CoreLocation;
using System.Reactive.Subjects;

namespace Shiny.Locations
{
    public class GeofenceManagerDelegate : ShinyLocationDelegate
    {
        readonly IGeofenceDelegate gdelegate;
        readonly RepositoryWrapper<GeofenceRegion, GeofenceRegionStore> repository;
        readonly Subject<GeofenceCurrentStatus> stateSubject;


        public GeofenceManagerDelegate()
        {
            this.gdelegate = ShinyHost.Resolve<IGeofenceDelegate>();
            this.repository = ShinyHost.Resolve<IRepository>().Wrap();
            this.stateSubject = new Subject<GeofenceCurrentStatus>();
        }


        public override void RegionEntered(CLLocationManager manager, CLRegion region)
            => this.Broadcast(manager, region, GeofenceState.Entered);


        public override void RegionLeft(CLLocationManager manager, CLRegion region)
            => this.Broadcast(manager, region, GeofenceState.Exited);


        public IObservable<GeofenceCurrentStatus> WhenStateDetermined() => this.stateSubject;
        public override async void DidDetermineState(CLLocationManager manager, CLRegionState state, CLRegion region)
        {
            if (region is CLCircularRegion native)
            {
                var geofence = await this.repository.Get(native.Identifier);
                if (geofence != null)
                {
                    var args = new GeofenceCurrentStatus(geofence, state.FromNative());
                    this.stateSubject.OnNext(args);
                }
            }
        }


        async void Broadcast(CLLocationManager manager, CLRegion region, GeofenceState status)
        {
            try
            {
                if (region is CLCircularRegion native)
                {
                    var geofence = await this.repository.Get(native.Identifier);

                    if (geofence != null)
                    {
                        this.gdelegate.OnStatusChanged(status, geofence);
                        if (geofence.SingleUse)
                        {
                            await this.repository.Remove(geofence.Identifier);
                            manager.StopMonitoring(native);
                        }
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
