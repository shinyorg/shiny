using System;
using CoreBluetooth;
using Foundation;
using Shiny.Logging;


namespace Shiny.BluetoothLE.Central.Delegates
{
    public class ShinyCbCentralManagerDelegate : CBCentralManagerDelegate
    {
        readonly IBleAdapterDelegate adapterDelegate;
        readonly IBlePeripheralDelegate peripheralDelegate;
        readonly IMessageBus messageBus;


        public ShinyCbCentralManagerDelegate()
        {
            this.messageBus = ShinyHost.Resolve<IMessageBus>();
            this.peripheralDelegate = ShinyHost.Resolve<IBlePeripheralDelegate>();
            this.adapterDelegate = ShinyHost.Resolve<IBleAdapterDelegate>();
        }


        public override void ConnectedPeripheral(CBCentralManager central, CBPeripheral peripheral)
        {
        }


        public override void DisconnectedPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error)
        {
        }


        public override void DiscoveredPeripheral(CBCentralManager central, CBPeripheral peripheral, NSDictionary advertisementData, NSNumber RSSI)
        {
        }


        public override void FailedToConnectPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error)
        {
        }


        public override void RetrievedConnectedPeripherals(CBCentralManager central, CBPeripheral[] peripherals)
        {
        }


        public override void RetrievedPeripherals(CBCentralManager central, CBPeripheral[] peripherals)
        {
        }


        public override void WillRestoreState(CBCentralManager central, NSDictionary dict)
        {
            // TODO: restore scan? CBCentralManager.RestoredStateScanOptionsKey
            if (this.peripheralDelegate == null)
                return;

            var peripheralArray = (NSArray)dict[CBCentralManager.RestoredStatePeripheralsKey];
            for (nuint i = 0; i < peripheralArray.Count; i++)
            {
                var item = peripheralArray.GetItem<CBPeripheral>(i);
                //var peripheral = this.GetPeripheral(item);
                this.Execute(() => this.peripheralDelegate.OnConnected(null)); // TODO
            }
        }


        public override void UpdatedState(CBCentralManager central)
        {
            var state = central.State.FromNative();
            this.Execute(
                () => this.adapterDelegate?.OnBleAdapterStateChanged(state),
                () => state
            );
        }


        protected virtual void Execute<TMsg>(Action delegateRun, Func<TMsg> msgBus)
        {
            this.Execute(() => delegateRun());
            this.Execute(() =>
            {
                var msg = msgBus();
                this.messageBus.Publish(msg);
            });
        }


        protected virtual void Execute(Action action)
        {
            try
            {
                action();
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }
    }
}
/*
 public AdapterContext(BleAdapterConfiguration config)
        {
            config = config ?? new BleAdapterConfiguration();
            var opts = new CBCentralInitOptions
            {
                ShowPowerAlert = config.ShowPowerAlert
            };

            if (!config.RestoreIdentifier.IsEmpty())
                opts.RestoreIdentifier = config.RestoreIdentifier;

            this.Manager = new CBCentralManager(this, config.DispatchQueue, opts);
        }


        public CBCentralManager Manager { get; }


        public IPeripheral GetPeripheral(CBPeripheral peripheral) => this.peripherals.GetOrAdd(
            peripheral.Identifier.ToString(),
            x => new Peripheral(this, peripheral)
        );


        public IEnumerable<IPeripheral> GetConnectedDevices() => this.peripherals
            .Where(x =>
                x.Value.Status == ConnectionState.Connected ||
                x.Value.Status == ConnectionState.Connecting
            )
            .Select(x => x.Value);


        public void Clear() => this.peripherals
            .Where(x => x.Value.Status != ConnectionState.Connected)
            .ToList()
            .ForEach(x => this.peripherals.TryRemove(x.Key, out var device));




        public Subject<CBPeripheral> PeripheralConnected { get; } = new Subject<CBPeripheral>();
        public override void ConnectedPeripheral(CBCentralManager central, CBPeripheral peripheral) => this.PeripheralConnected.OnNext(peripheral);


        public Subject<CBPeripheral> PeripheralDisconnected { get; } = new Subject<CBPeripheral>();
        public override void DisconnectedPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error) => this.PeripheralDisconnected.OnNext(peripheral);


        public Subject<ScanResult> ScanResultReceived { get; } = new Subject<ScanResult>();
        public override void DiscoveredPeripheral(CBCentralManager central, CBPeripheral peripheral, NSDictionary advertisementData, NSNumber rssi)
            => this.ScanResultReceived.OnNext(new ScanResult(
                this.GetPeripheral(peripheral),
                rssi?.Int32Value ?? 0,
                new AdvertisementData(advertisementData)
            ));


        public Subject<PeripheralConnectionFailed> FailedConnection { get; } = new Subject<PeripheralConnectionFailed>();
        public override void FailedToConnectPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error)
            => this.FailedConnection.OnNext(new PeripheralConnectionFailed(peripheral, error));


        public Subject<AccessState> StateUpdated { get; } = new Subject<AccessState>();
        public override void UpdatedState(CBCentralManager central)
        {

        }
     */
