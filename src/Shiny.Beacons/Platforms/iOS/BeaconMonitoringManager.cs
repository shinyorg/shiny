using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CoreLocation;
using Shiny.Infrastructure;
using Shiny.Locations;


namespace Shiny.Beacons
{
    public partial class BeaconMonitoringManager : IBeaconMonitoringManager
    {
        readonly IRepository repository;
        readonly CLLocationManager manager;
        readonly BeaconLocationManagerDelegate gdelegate;


        public BeaconMonitoringManager(IServiceProvider services, IRepository repository)
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
            await this.repository.Set(region.Identifier, region);
            this.manager.StartMonitoring(region.ToNative());
        }


        public async Task StopMonitoring(string identifier)
        {
            var region = await this.repository.Get<BeaconRegion>(identifier);
            if (region != null)
            {
                await this.repository.Remove<BeaconRegion>(region.Identifier);
                this.manager.StopMonitoring(region.ToNative());
            }
        }


        public async Task StopAllMonitoring()
        {
            await this.repository.Clear<BeaconRegion>();
            var allRegions = this
               .manager
               .MonitoredRegions
               .OfType<CLBeaconRegion>();

            foreach (var region in allRegions)
                this.manager.StopMonitoring(region);
        }

        public async Task<IEnumerable<BeaconRegion>> GetMonitoredRegions()
            => await this.repository.GetAll<BeaconRegion>();
    }
}