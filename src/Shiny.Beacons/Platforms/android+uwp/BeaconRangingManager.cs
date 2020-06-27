using System;
using System.Threading.Tasks;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Shiny.BluetoothLE;
using Shiny.Infrastructure;


namespace Shiny.Beacons
{
    public class BeaconRangingManager : IBeaconRangingManager //, IBeaconMonitoringManager
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
/*
 *  if (this.AdvertisedBeacon != null)
        throw new ArgumentException("You are already advertising a beacon");

    var settings = new AdvertiseSettings.Builder()
        .SetAdvertiseMode(AdvertiseMode.Balanced)
        .SetConnectable(false);

    var adData = new AdvertiseData.Builder()
        .AddManufacturerData(0x004C, beacon.ToIBeaconPacket(10)); // Apple

    this.manager
        .Adapter
        .BluetoothLeAdvertiser
        .StartAdvertising(
            settings.Build(),
            adData.Build(),
            this.adCallbacks
        );

    this.AdvertisedBeacon = beacon;
 */
