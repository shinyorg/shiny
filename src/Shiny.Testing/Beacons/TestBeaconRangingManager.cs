using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Shiny.Beacons;


namespace Shiny.Testing.Beacons
{
    public class TestBeaconRangingManager : IBeaconRangingManager
    {
        public AccessState CurrentAccessState { get; set; } = AccessState.Available;
        public Task<AccessState> RequestAccess() => Task.FromResult(this.CurrentAccessState);
        public Subject<Beacon> BeaconSubject { get; } = new Subject<Beacon>();
        public IObservable<Beacon> WhenBeaconRanged(BeaconRegion region)
            => this.BeaconSubject.Where(region.IsBeaconInRegion);
    }
}
