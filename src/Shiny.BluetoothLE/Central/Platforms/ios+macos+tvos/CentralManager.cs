using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using CoreBluetooth;


namespace Shiny.BluetoothLE.Central
{
    public partial class CentralManager : AbstractCentralManager
    {
        readonly AdapterContext context;


        public override string AdapterName => "Default Bluetooth Peripheral";
        public override bool IsScanning => this.context.Manager.IsScanning;


        public override IObservable<AccessState> RequestAccess() => Observable.Create<AccessState>(ob =>
        {
            // TODO: check info.plist?
            IDisposable disp = null;
            if (this.context.Manager.State == CBCentralManagerState.Unknown)
            {
                disp = this.context
                    .StateUpdated
                    .Subscribe(_ =>
                    {
                        var current = this.context.Manager.State.FromNative();
                        ob.Respond(current);
                    });
            }
            else
            {
                ob.Respond(this.context.Manager.State.FromNative());
            }

            return () => disp?.Dispose();
        });


        public override AccessState Status => this.context.Manager.State.FromNative();


        public override IObservable<IPeripheral> GetKnownPeripheral(Guid deviceId)
        {
            var uuid = deviceId.ToNSUuid();
            var peripheral = this.context
                .Manager
                .RetrievePeripheralsWithIdentifiers(uuid)
                .FirstOrDefault();

            if (peripheral == null)
                return Observable.Return<IPeripheral>(null);

            var device = this.context.GetDevice(peripheral);
            return Observable.Return(device);
        }


        public override IObservable<IEnumerable<IPeripheral>> GetPairedPeripherals() => Observable.Return(new IPeripheral[0]);

        public override IObservable<IEnumerable<IPeripheral>> GetConnectedPeripherals(Guid? serviceUuid = null)
        {
            if (serviceUuid == null)
                return Observable.Return(this.context.GetConnectedDevices().ToList());

            var list = new List<IPeripheral>();
            var peripherals = this.context.Manager.RetrieveConnectedPeripherals(serviceUuid.Value.ToCBUuid());
            foreach (var peripheral in peripherals)
            {
                var dev = this.context.GetDevice(peripheral);
                list.Add(dev);
            }
            return Observable.Return(list);
        }


        public override IObservable<AccessState> WhenStatusChanged() => this.context
            .StateUpdated
            .Select(_ => this.Status)
            .StartWith(this.Status);


        public override IObservable<IScanResult> Scan(ScanConfig config)
        {
            config = config ?? new ScanConfig();

            if (this.IsScanning)
                throw new ArgumentException("There is already an existing scan");

            //if (config.ScanType == BleScanType.Background && (config.ServiceUuids == null || config.ServiceUuids.Count == 0))
            //    throw new ArgumentException("Background scan type set but not ServiceUUID");

            this.context.Clear();
            return this.context
                .Manager
                .WhenReady()
                .Select(_ => Observable.Create<IScanResult>(ob =>
                {
                    var scan = this.context
                        .ScanResultReceived
                        .AsObservable()
                        .Subscribe(ob.OnNext);

                    if (config.ServiceUuids == null || config.ServiceUuids.Count == 0)
                    {
                        this.context.Manager.ScanForPeripherals(null, new PeripheralScanningOptions { AllowDuplicatesKey = true });
                    }
                    else
                    {
                        var uuids = config.ServiceUuids.Select(o => o.ToCBUuid()).ToArray();
                        this.context.Manager.ScanForPeripherals(uuids, new PeripheralScanningOptions { AllowDuplicatesKey = true });
                    }

                    return () =>
                    {
                        this.context.Manager.StopScan();
                        scan.Dispose();
                    };
                }))
                .Switch();
        }


        public override void StopScan() => this.context.Manager.StopScan();
	}
}