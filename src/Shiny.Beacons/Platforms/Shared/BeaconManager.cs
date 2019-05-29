#if WINDOWS_UWP || __ANDROID__
using System;
using System.Threading.Tasks;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Shiny.BluetoothLE.Central;
using Shiny.Infrastructure;


namespace Shiny.Beacons
{
    public class BeaconManager : AbstractBeaconManager
    {
        readonly ICentralManager centralManager;


        public BeaconManager(ICentralManager centralManager, IRepository repository) : base(repository)
        {
            this.centralManager = centralManager;
        }


        public override AccessState GetCurrentStatus(bool background) => this.centralManager.Status;
        public override Task<AccessState> RequestAccess(bool monitoring)
            => this.centralManager.RequestAccess().ToTask();

        public override IObservable<Beacon> WhenBeaconRanged(BeaconRegion region)
            => this.Scan().Where(region.IsBeaconInRegion);

        public override Task StartMonitoring(BeaconRegion region)
            => this.Repository.Set(region.Identifier, region);

        public override Task StopMonitoring(BeaconRegion region)
            => this.Repository.Remove<BeaconRegion>(region.Identifier);

        public override Task StopAllMonitoring()
            => this.Repository.Clear<BeaconRegion>();


        IObservable<Beacon> beaconScanner;
        protected IObservable<Beacon> Scan()
        {
            this.beaconScanner = this.beaconScanner ?? this.centralManager
                .Scan()
                .Where(x => x.IsBeacon())
                .Select(x => x.ToBeacon())
                .Publish()
                .RefCount();

            return this.beaconScanner;
        }


        public override IObservable<AccessState> WhenAccessStatusChanged(bool monitoring)
        {
            throw new NotImplementedException();
        }
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
#endif