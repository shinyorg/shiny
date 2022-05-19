using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CoreLocation;
using Shiny.Locations;
using Shiny.Stores;

namespace Shiny.Beacons;


public partial class BeaconMonitoringManager : IBeaconMonitoringManager
{
    readonly IRepository<BeaconRegion> repository;
    readonly CLLocationManager manager;
    readonly BeaconLocationManagerDelegate gdelegate;


    public BeaconMonitoringManager(IServiceProvider services, IRepository<BeaconRegion> repository)
    {
        this.repository = repository;
        this.gdelegate = new BeaconLocationManagerDelegate(services);
        this.manager = new CLLocationManager
        {
            Delegate = this.gdelegate
        };
    }


    public Task<AccessState> RequestAccess() => this.manager.RequestAccess(true);


    public async Task StartMonitoring(BeaconRegion region)
    {
        await this.repository.Set(region).ConfigureAwait(false);
        this.manager.StartMonitoring(region.ToNative());
    }


    public async Task StopMonitoring(string identifier)
    {
        var region = await this.repository
            .Get(identifier)
            .ConfigureAwait(false);

        if (region != null)
        {
            await this.repository
                .Remove(region.Identifier)
                .ConfigureAwait(false);
            this.manager.StopMonitoring(region.ToNative());
        }
    }


    public async Task StopAllMonitoring()
    {
        await this.repository
            .Clear()
            .ConfigureAwait(false);

        var allRegions = this
           .manager
           .MonitoredRegions
           .OfType<CLBeaconRegion>();

        foreach (var region in allRegions)
            this.manager.StopMonitoring(region);
    }

    public async Task<IEnumerable<BeaconRegion>> GetMonitoredRegions()
        => await this.repository.GetList().ConfigureAwait(false);
}