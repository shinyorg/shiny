using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Shiny.Beacons;


namespace Shiny.Testing.Beacons
{
    public class BeaconManager : IBeaconManager
    {
        readonly IList<BeaconRegion> monitoredRegions = new List<BeaconRegion>();


        AccessState accessStatus = AccessState.Available;
        public AccessState AccessStatus
        {
            get => this.accessStatus;
            set
            {
                this.accessStatus = value;
                this.accessStateSubject.OnNext(value);
            }
        }


        public AccessState GetCurrentStatus(bool forMonitoring) => this.AccessStatus;
        public Task<IEnumerable<BeaconRegion>> GetMonitoredRegions() => Task.FromResult(this.monitoredRegions.AsEnumerable());
        public Task<AccessState> RequestAccess(bool monitoring) => Task.FromResult(this.AccessStatus);


        public Task StartMonitoring(BeaconRegion region)
        {
            this.monitoredRegions.Add(region);
            return Task.CompletedTask;
        }

        public Task StopAllMonitoring()
        {
            this.monitoredRegions.Clear();
            return Task.CompletedTask;
        }


        public Task StopMonitoring(string identifier)
        {
            var region = this.monitoredRegions.FirstOrDefault(x => x.Identifier == identifier);
            if (region != null)
                this.monitoredRegions.Remove(region);

            return Task.CompletedTask;
        }


        readonly Subject<AccessState> accessStateSubject = new Subject<AccessState>();
        public IObservable<AccessState> WhenAccessStatusChanged(bool monitoring) => this.accessStateSubject;


        public Subject<Beacon> BeaconRangedSubject { get; } = new Subject<Beacon>();
        public IObservable<Beacon> WhenBeaconRanged(BeaconRegion region) => this.BeaconRangedSubject;
    }
}
