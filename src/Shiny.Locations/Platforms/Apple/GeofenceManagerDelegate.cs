﻿using System;
using System.Reactive.Subjects;
using System.Collections.Generic;
using CoreLocation;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Hosting;
using Shiny.Stores;

namespace Shiny.Locations;


public class GeofenceManagerDelegate : ShinyLocationDelegate
{
    //readonly RepositoryWrapper<GeofenceRegion, GeofenceRegionStore> repository;
    readonly Subject<GeofenceCurrentStatus> stateSubject = new();
    //readonly Lazy<IEnumerable<IGeofenceDelegate>> delegates = ShinyHost.LazyResolve<IEnumerable<IGeofenceDelegate>>();


    public GeofenceManagerDelegate()
    {
        this.repository = Host.Current.Services.GetRequiredService<IRepository>();
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
        if (region is CLCircularRegion native)
        {
            var geofence = await this.repository.Get(native.Identifier);
            if (geofence != null)
            {
                await this.delegates.Value.RunDelegates(x => x.OnStatusChanged(status, geofence));
                if (geofence.SingleUse)
                {
                    await this.repository.Remove(geofence.Identifier);
                    manager.StopMonitoring(native);
                }
            }
        }
    }
}
