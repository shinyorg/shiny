using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny.Beacons
{
    public abstract class AbstractBeaconManager : IBeaconManager
    {
        protected AbstractBeaconManager(IRepository repository)
        {
            this.Repository = new RepositoryWrapper<BeaconRegion, BeaconRegionStore>(
                repository,
                args => new BeaconRegionStore
                {
                    Identifier = args.Identifier,
                    Uuid = args.Uuid,
                    Major = args.Major,
                    Minor = args.Minor
                },
                store => new BeaconRegion(
                    store.Identifier,
                    store.Uuid,
                    store.Major,
                    store.Minor
                )
            );
        }


        protected RepositoryWrapper<BeaconRegion, BeaconRegionStore> Repository { get; }

        public abstract AccessState GetCurrentStatus(bool monitoring);
        public abstract IObservable<AccessState> WhenAccessStatusChanged(bool monitoring);
        public abstract IObservable<Beacon> WhenBeaconRanged(BeaconRegion region);
        public abstract Task<AccessState> RequestAccess(bool monitoring);
        public abstract Task StartMonitoring(BeaconRegion region);
        public abstract Task StopMonitoring(BeaconRegion region);
        public abstract Task StopAllMonitoring();
        public virtual async Task<IEnumerable<BeaconRegion>> GetMonitoredRegions() => await this.Repository.GetAll();
    }
}
