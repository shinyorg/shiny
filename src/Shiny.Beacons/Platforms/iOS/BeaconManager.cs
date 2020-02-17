using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using CoreLocation;
using Shiny.Infrastructure;


namespace Shiny.Beacons
{
    public class BeaconManager : AbstractBeaconManager
    {
        readonly CLLocationManager manager;
        readonly BeaconLocationManagerDelegate gdelegate;


        public BeaconManager(IRepository repository) : base(repository)
        {
            this.gdelegate = new BeaconLocationManagerDelegate();
            this.manager = new CLLocationManager
            {
                Delegate = this.gdelegate
            };
        }


        public override AccessState GetCurrentStatus(bool background) => this.manager.GetCurrentStatus<CLBeaconRegion>(background);
        public override IObservable<AccessState> WhenAccessStatusChanged(bool background) => this.manager.WhenAccessStatusChanged(background);
        public override Task<AccessState> RequestAccess(bool monitoring) => this.manager.RequestAccess(monitoring);


        public override IObservable<Beacon> WhenBeaconRanged(BeaconRegion region)
        {
            var native = region.ToNative();
            this.manager.StartRangingBeacons(native);

            return this.gdelegate
                .WhenBeaconRanged()
                .Where(region.IsBeaconInRegion)
                .Finally(() =>
                    this.manager.StopRangingBeacons(native)
                );
        }


        public override async Task StartMonitoring(BeaconRegion region)
        {
            await this.Repository.Set(region.Identifier, region);
            this.manager.StartMonitoring(region.ToNative());
        }


        public override async Task StopMonitoring(string identifier)
        {
            var region = await this.Repository.Get<BeaconRegion>(identifier);
            if (region == null)
                return;

            await this.Repository.Remove<BeaconRegion>(region.Identifier);
            this.manager.StopMonitoring(region.ToNative());
        }


        public override async Task StopAllMonitoring()
        {
            await this.Repository.Clear<BeaconRegion>();
            var allRegions = this
               .manager
               .MonitoredRegions
               .OfType<CLBeaconRegion>();

            foreach (var region in allRegions)
                this.manager.StopMonitoring(region);
        }
    }
}