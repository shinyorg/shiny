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


        public AdapterContext(BleAdapterConfiguration config)
        {
            var opts = new CBCentralInitOptions
            {
                ShowPowerAlert = config.ShowPowerAlert
            };

#if __IOS__
            if (!config.RestoreIdentifier.IsEmpty())
                opts.RestoreIdentifier = config?.RestoreIdentifier;
#endif
           this.Manager = new CBCentralManager(this, config?.DispatchQueue, opts);
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


        public override void WillRestoreState(CBCentralManager central, NSDictionary dict)
        {
#if __IOS__
            // TODO: restore scan? CBCentralManager.RestoredStateScanOptionsKey

            var peripheralArray = (NSArray)dict[CBCentralManager.RestoredStatePeripheralsKey];
            Log.Write(BleLogCategory.StateRestore, $"Restoring peripheral state on {peripheralArray.Count} peripherals");

            if (peripheralArray.Count > 0)
            {
                var bleDelegate = ShinyHost.Resolve<IBleStateRestoreDelegate>();
                if (bleDelegate != null)
                {
                    for (nuint i = 0; i < peripheralArray.Count; i++)
                    {
                        try
                        {
                            var item = peripheralArray.GetItem<CBPeripheral>(i);
                            var dev = this.GetPeripheral(item);

                            bleDelegate.OnConnected(dev);
                        }
                        catch (Exception ex)
                        {
                            Log.Write(ex);
                        }
                    }
                }
            }
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
        public override void UpdatedState(CBCentralManager central)
        {
            var state = central.State.FromNative();
            this.StateUpdated.OnNext(state);

            try
            {
                var adapterDelegate = ShinyHost.Resolve<IBleAdapterDelegate>();
                if (adapterDelegate != null)
                {
                    adapterDelegate.OnBleAdapterStateChanged(state);
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }
    }
}