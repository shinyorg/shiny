using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using CoreBluetooth;
using Foundation;
using Shiny.Logging;


namespace Shiny.BluetoothLE.Central
{
    public class AdapterContext : CBCentralManagerDelegate
    {
        readonly ConcurrentDictionary<string, IPeripheral> peripherals = new ConcurrentDictionary<string, IPeripheral>();
        readonly IServiceProvider services;


        public AdapterContext(IServiceProvider services, BleAdapterConfiguration config)
        {
            this.services = services;

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


        public override async void WillRestoreState(CBCentralManager central, NSDictionary dict)
        {
#if __IOS__
            try
            {
                var del = this.services.GetService<IBlePeripheralDelegate>();

                var peripheralArray = (NSArray)dict[CBCentralManager.RestoredStatePeripheralsKey];
                for (nuint i = 0; i < peripheralArray.Count; i++)
                {
                    var item = peripheralArray.GetItem<CBPeripheral>(i);
                    var peripheral = this.GetPeripheral(item);
                    await del.OnConnected(peripheral);
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
            // TODO: restore scan? CBCentralManager.RestoredStateScanOptionsKey
#endif
        }


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
        public override async void UpdatedState(CBCentralManager central)
        {
            var state = central.State.FromNative();

            if (state == AccessState.Unknown)
                return;

            this.StateUpdated.OnNext(state);
            try
            {
                await this.services
                    .GetService<IBleAdapterDelegate>()?
                    .OnBleAdapterStateChanged(state);
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }
    }
}