//using System;
//using System.Threading.Tasks;
//using System.Linq;
//using System.Reactive.Linq;
//using System.Reactive.Threading.Tasks;
//using Shiny.BluetoothLE;
//using Shiny.Infrastructure;


//namespace Shiny.Beacons
//{
//    public class BeaconRangingManager : IBeaconRangingManager //, IBeaconMonitoringManager
//    {
//        readonly IRepository repository;
//        readonly IBleManager centralManager;
//        readonly IMessageBus messageBus;
//        IObservable<Beacon>? beaconScanner;


//        public BeaconRangingManager(IBleManager centralManager,
//                             IMessageBus messageBus,
//                             IRepository repository)
//        {
//            this.centralManager = centralManager;
//            this.messageBus = messageBus;
//            this.repository = repository;
//        }


//        public AccessState GetCurrentStatus(bool background) => this.centralManager.Status;
//        public Task<AccessState> RequestAccess(bool monitoring) => this.centralManager.RequestAccess().ToTask();
//        public IObservable<Beacon> WhenBeaconRanged(BeaconRegion region) => this.Scan().Where(region.IsBeaconInRegion);
//        public IObservable<AccessState> WhenAccessStatusChanged(bool monitoring) => this.centralManager.WhenStatusChanged();


//        public async Task StartMonitoring(BeaconRegion region)
//        {
//            var stored = await this.repository.Set(region.Identifier, region);
//            var eventType = stored ? BeaconRegisterEventType.Add : BeaconRegisterEventType.Update;
//            this.messageBus.Publish(new BeaconRegisterEvent(eventType, region));
//        }


//        public async Task StopMonitoring(string identifier)
//        {
//            var region = await this.repository.Get<BeaconRegion>(identifier);
//            if (region != null)
//            {
//                await this.repository.Remove<BeaconRegion>(identifier);
//                this.messageBus.Publish(new BeaconRegisterEvent(BeaconRegisterEventType.Remove, region));
//            }
//        }

//        public async Task StopAllMonitoring()
//        {
//            await this.repository.Clear<BeaconRegion>();
//            this.messageBus.Publish(new BeaconRegisterEvent(BeaconRegisterEventType.Clear));
//        }


//        protected IObservable<Beacon> Scan() => this.beaconScanner ??= this.centralManager
//            .ScanForBeacons(false)
//            .Publish()
//            .RefCount();
//    }
//}
///*
// *  if (this.AdvertisedBeacon != null)
//        throw new ArgumentException("You are already advertising a beacon");

//    var settings = new AdvertiseSettings.Builder()
//        .SetAdvertiseMode(AdvertiseMode.Balanced)
//        .SetConnectable(false);

//    var adData = new AdvertiseData.Builder()
//        .AddManufacturerData(0x004C, beacon.ToIBeaconPacket(10)); // Apple

//    this.manager
//        .Adapter
//        .BluetoothLeAdvertiser
//        .StartAdvertising(
//            settings.Build(),
//            adData.Build(),
//            this.adCallbacks
//        );

//    this.AdvertisedBeacon = beacon;
// */
