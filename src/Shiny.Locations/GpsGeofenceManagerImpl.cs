﻿using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Locations;


public class GpsGeofenceManagerImpl : IGeofenceManager, IShinyStartupTask
{
    readonly IGpsManager gpsManager;


    static readonly GpsRequest defaultRequest = new GpsRequest
    {
        BackgroundMode = GpsBackgroundMode.Realtime,
        Accuracy = GpsAccuracy.Normal
    };


    public GpsGeofenceManagerImpl(IRepository repository, IGpsManager gpsManager) : base(repository)
        => this.gpsManager = gpsManager;


    public async void Start()
    {
        var restore = await this.GetMonitorRegions();
        if (restore.Any())
            await this.TryStartGps();
    }


    public override Task<AccessState> RequestAccess()
        => this.gpsManager.RequestAccess(defaultRequest);


    public override async Task<GeofenceState> RequestState(GeofenceRegion region, CancellationToken cancelToken = default)
    {
        var reading = await this.gpsManager!
            .GetLastReading()
            .Timeout(TimeSpan.FromSeconds(10))
            .ToTask();

        if (reading == null)
            return GeofenceState.Unknown;

        var state = region.IsPositionInside(reading.Position)
            ? GeofenceState.Entered
            : GeofenceState.Exited;

        return state;
    }


    public override async Task StartMonitoring(GeofenceRegion region)
    {
        await this.TryStartGps();
        await this.Repository.Set(region.Identifier, region);
    }


    public override async Task StopAllMonitoring()
    {
        await this.Repository.Clear();
        await this.gpsManager.StopListener();
    }


    public override async Task StopMonitoring(string identifier)
    {
        await this.Repository.Remove(identifier);
        var geofences = await this.Repository.GetAll();

        if (geofences.Count == 0)
            await this.gpsManager!.StopListener();
    }


    protected async Task TryStartGps()
    {
        if (this.gpsManager.CurrentListener == null)
            await this.gpsManager.StartListener(defaultRequest);
    }
}