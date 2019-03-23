using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using CoreLocation;


namespace Shiny.Beacons
{
    public class BeaconManager : AbstractBeaconManager
    {
        readonly CLLocationManager manager;
        readonly BeaconLocationManagerDelegate gdelegate;

        public BeaconManager(IRepository repository) : base(repository)
        {
            this.gdelegate = new BeaconLocationManagerDelegate();
            this.manager = new CLLocationManager();
        }


        public override AccessState Status
        {
            get
            {
                if (!CLLocationManager.LocationServicesEnabled)
                    return AccessState.Disabled;

                if (!CLLocationManager.IsMonitoringAvailable(typeof(CLCircularRegion)))
                    return AccessState.NotSupported;

                return CLLocationManager.Status.FromNative(true);
            }
        }


        public override async Task<AccessState> RequestAccess(bool monitoring)
        {
            if (!CLLocationManager.LocationServicesEnabled)
                return AccessState.Disabled;

            if (!CLLocationManager.IsMonitoringAvailable(typeof(CLCircularRegion)))
                return AccessState.NotSupported;

            var result = CLLocationManager.Status;
            if (result == CLAuthorizationStatus.NotDetermined)
            {
                var tcs = new TaskCompletionSource<CLAuthorizationStatus>();
                var handler = new EventHandler<CLAuthorizationStatus>((sender, status) => tcs.TrySetResult(status));
                this.gdelegate.AuthStatusChanged += handler;

                if (monitoring)
                    this.manager.RequestAlwaysAuthorization();
                else
                    this.manager.RequestWhenInUseAuthorization();

                result = await tcs.Task;
                this.gdelegate.AuthStatusChanged -= handler;
            }
            return result.FromNative(monitoring);
        }


        public override IObservable<Beacon> WhenBeaconRanged(BeaconRegion region)
        {
            var nativeRegion = this.ToNative(region);
            this.manager.StartRangingBeacons(nativeRegion);

            return this.Scan()
                .Where(region.IsBeaconInRegion)
                .Finally(() =>
                    this.manager.StopRangingBeacons(nativeRegion)
                );
        }


        public override async Task StartMonitoring(BeaconRegion region)
        {
            await this.Repository.Set(region.Identifier, region);
            var native = this.ToNative(region);
            this.manager.StartMonitoring(native);
        }


        public override async Task StopMonitoring(BeaconRegion region)
        {
            await this.Repository.Remove(region.Identifier);
            var native = this.ToNative(region);
            this.manager.StopMonitoring(native);
        }


        public override async Task StopAllMonitoring()
        {
            await this.Repository.Clear();
            var allRegions = this
               .manager
               .MonitoredRegions
               .OfType<CLBeaconRegion>();

            foreach (var region in allRegions)
                this.manager.StopMonitoring(region);
        }


        IObservable<Beacon> beaconScanner;
        protected IObservable<Beacon> Scan()
        {
            this.beaconScanner = this.beaconScanner ?? Observable.Create<Beacon>(ob =>
            {
                var handler = new EventHandler<CLRegionBeaconsRangedEventArgs>((sender, args) =>
                {
                    foreach (var native in args.Beacons)
                    {
                        // TODO: load up all beacons if we haven't done so already
                        ob.OnNext(new Beacon
                        (
                            native.ProximityUuid.ToGuid(),
                            native.Major.UInt16Value,
                            native.Minor.UInt16Value,
                            native.Accuracy,
                            this.FromNative(native.Proximity)
                        ));
                    }
                });
                this.manager.DidRangeBeacons += handler;
                return () => this.manager.DidRangeBeacons -= handler;
            })
            .Publish()
            .RefCount();

            return this.beaconScanner;
        }


        protected CLBeaconRegion ToNative(BeaconRegion region)
        {
            if (region.Uuid == null)
                throw new ArgumentException("You must pass a UUID for the Beacon Region");

            var uuid = region.Uuid.ToNSUuid();
            CLBeaconRegion native = null;

            if (region.Major > 0 && region.Minor > 0)
                native = new CLBeaconRegion(uuid, region.Major.Value, region.Minor.Value, region.Identifier);

            else if (region.Major > 0)
                native = new CLBeaconRegion(uuid, region.Major.Value, region.Identifier);

            else
                native = new CLBeaconRegion(uuid, region.Identifier);

            //native.NotifyEntryStateOnDisplay = true;
            native.NotifyOnEntry = true;
            native.NotifyOnExit = true;

            return native;
        }


        protected Proximity FromNative(CLProximity proximity)
        {
            switch (proximity)
            {
                case CLProximity.Far:
                    return Proximity.Far;

                case CLProximity.Immediate:
                    return Proximity.Immediate;

                case CLProximity.Near:
                    return Proximity.Near;

                case CLProximity.Unknown:
                default:
                    return Proximity.Unknown;
            }
        }


        protected CLProximity ToNative(Proximity proximity)
        {
            switch (proximity)
            {
                case Proximity.Far:
                    return CLProximity.Far;

                case Proximity.Immediate:
                    return CLProximity.Immediate;

                case Proximity.Near:
                    return CLProximity.Near;

                case Proximity.Unknown:
                default:
                    return CLProximity.Unknown;
            }
        }
    }
}