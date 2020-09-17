using System;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Shiny.BluetoothLE;


namespace Shiny.Beacons
{
    public class BeaconRangingManager : IBeaconRangingManager
    {
        readonly IBleManager centralManager;
        IObservable<Beacon>? beaconScanner;


        public BeaconRangingManager(IBleManager centralManager)
            => this.centralManager = centralManager;


        public Task<AccessState> RequestAccess() => this.centralManager.RequestAccess().ToTask();
        public IObservable<Beacon> WhenBeaconRanged(BeaconRegion region) => this.beaconScanner ??= this.centralManager
            .ScanForBeacons(false)
            .Publish()
            .RefCount();
    }
}