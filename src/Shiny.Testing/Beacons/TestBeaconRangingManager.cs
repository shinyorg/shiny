using System;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Shiny.Beacons;


namespace Shiny.Testing.Beacons
{
    public class BeaconRangingManager : IBeaconRangingManager
    {
        public AccessState AccessStatus { get; set; } = AccessState.Available;
        public Task<AccessState> RequestAccess() => Task.FromResult(this.AccessStatus);

        public Subject<Beacon> BeaconRangedSubject { get; } = new Subject<Beacon>();
        public IObservable<Beacon> WhenBeaconRanged(BeaconRegion region) => this.BeaconRangedSubject;
    }
}
