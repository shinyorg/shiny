using System;
using System.Reactive.Subjects;
using Shiny.Infrastructure;
using CoreLocation;


namespace Shiny.Locations
{
    public class GeofenceManagerDelegate : ShinyLocationDelegate
    {
        readonly RepositoryWrapper<GeofenceRegion, GeofenceRegionStore> repository;
        readonly Subject<GeofenceCurrentStatus> stateSubject;
        readonly Lazy<IGeofenceDelegate> gdelegate = new Lazy<IGeofenceDelegate>(() => ShinyHost.Resolve<IGeofenceDelegate>());


        public GeofenceManagerDelegate()
        {
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


        void Broadcast(CLLocationManager manager, CLRegion region, GeofenceState status) => Dispatcher.ExecuteBackgroundTask(async () =>
        {
            if (region is CLCircularRegion native)
            {
                var geofence = await this.repository.Get(native.Identifier);
                if (geofence != null)
                {
                    await this.gdelegate.Value.OnStatusChanged(status, geofence);
                    if (geofence.SingleUse)
                    {
                        await this.repository.Remove(geofence.Identifier);
                        manager.StopMonitoring(native);
                    }
                }
            }
        });
    }
}
