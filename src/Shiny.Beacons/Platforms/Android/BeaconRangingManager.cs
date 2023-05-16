using System;
using System.Threading.Tasks;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Shiny.BluetoothLE;

namespace Shiny.Beacons;


public class BeaconRangingManager : IBeaconRangingManager
{
    readonly IBleManager centralManager;
    readonly IObservable<Beacon> scanner;


    public BeaconRangingManager(IBleManager centralManager)
    {
        this.centralManager = centralManager;
        this.scanner = this.centralManager
            .ScanForBeacons(false)
            .Publish()
            .RefCount();
    }


    public Task<AccessState> RequestAccess() => this.centralManager.RequestAccess().ToTask();
    public IObservable<Beacon> WhenBeaconRanged(BeaconRegion region)
        => this.scanner.Where(beacon => region.IsBeaconInRegion(beacon));
}