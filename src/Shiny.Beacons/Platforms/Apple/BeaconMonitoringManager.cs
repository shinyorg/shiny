using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using CoreLocation;
using Microsoft.Extensions.Logging;
using Shiny.Locations;
using Shiny.Support.Repositories;

namespace Shiny.Beacons;


public partial class BeaconMonitoringManager : IBeaconMonitoringManager
{
    readonly IRepository repository;
    readonly CLLocationManager manager;
    readonly BeaconLocationManagerDelegate gdelegate;


    public BeaconMonitoringManager(
        IServiceProvider services,
        ILogger<BeaconLocationManagerDelegate> logger,
        IRepository repository
    )
    {
        this.repository = repository;
        this.gdelegate = new BeaconLocationManagerDelegate(services, logger);
        this.manager = new CLLocationManager
        {
            Delegate = this.gdelegate
        };
    }


    public Task<AccessState> RequestAccess() => this.manager.RequestAccess(true);


    public Task StartMonitoring(BeaconRegion region)
    {
        this.repository.Set(region);
        this.manager.StartMonitoring(region.ToNative());
        return Task.CompletedTask;
    }


    public void StopMonitoring(string identifier)
    {
        var region = this.repository.Get<BeaconRegion>(identifier);

        if (region != null)
        {
            this.repository.Remove<BeaconRegion>(region.Identifier);
            this.manager.StopMonitoring(region.ToNative());
        }
    }


    public void StopAllMonitoring()
    {
        this.repository.Clear<BeaconRegion>();

        var allRegions = this
           .manager
           .MonitoredRegions
           .OfType<CLBeaconRegion>();

        foreach (var region in allRegions)
            this.manager.StopMonitoring(region);
    }

    public IList<BeaconRegion> GetMonitoredRegions()
        => this.repository.GetList<BeaconRegion>();
}