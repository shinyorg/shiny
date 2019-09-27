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
        readonly IMessageBus messageBus;


        public BeaconManager(ICentralManager centralManager,
                              IMessageBus messageBus,
                              IRepository repository) : base(repository)
        {
            this.centralManager = centralManager;
            this.messageBus = messageBus;
        }


        public override AccessState GetCurrentStatus(bool background) => this.centralManager.Status;
        public override Task<AccessState> RequestAccess(bool monitoring)
            => this.centralManager.RequestAccess().ToTask();

        public override IObservable<Beacon> WhenBeaconRanged(BeaconRegion region)
            => this.Scan().Where(region.IsBeaconInRegion);


        public override async Task StartMonitoring(BeaconRegion region)
        {
            var stored = await this.Repository.Set(region.Identifier, region);
            var eventType = stored ? BeaconRegisterEventType.Add : BeaconRegisterEventType.Update;
            this.messageBus.Publish(new BeaconRegisterEvent(eventType, region));
        }

        public override async Task StopMonitoring(string identifier)
        {
            var region = await this.Repository.Get<BeaconRegion>(identifier);
            if (region != null)
            {
                await this.Repository.Remove<BeaconRegion>(identifier);
                this.messageBus.Publish(new BeaconRegisterEvent(BeaconRegisterEventType.Remove, region));
            }
        }

        public override async Task StopAllMonitoring()
        {
            await this.Repository.Clear<BeaconRegion>();
            this.messageBus.Publish(new BeaconRegisterEvent(BeaconRegisterEventType.Clear));
        }


        IObservable<Beacon> beaconScanner;
        protected IObservable<Beacon> Scan()
        {
            this.beaconScanner = this.beaconScanner ?? this.centralManager
                .ScanForBeacons(false)
                .Publish()
                .RefCount();

            return this.beaconScanner;
        }


        public override IObservable<AccessState> WhenAccessStatusChanged(bool monitoring)
            => this.centralManager.WhenStatusChanged();
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
