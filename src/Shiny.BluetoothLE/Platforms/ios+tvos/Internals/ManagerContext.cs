using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Subjects;
using CoreBluetooth;
using Foundation;
using Microsoft.Extensions.Logging;


namespace Shiny.BluetoothLE.Internals
{
    public class ManagerContext : CBCentralManagerDelegate
    {
        readonly ConcurrentDictionary<string, IPeripheral> peripherals = new ConcurrentDictionary<string, IPeripheral>();
        readonly Lazy<CBCentralManager> managerLazy;
        readonly ILogger logger;


        public ManagerContext(IServiceProvider services,
                              BleConfiguration config,
                              ILogger<IBleManager> logger)
        {
            this.Services = services;
            this.logger = logger;

            this.managerLazy = new Lazy<CBCentralManager>(() =>
            {
                if (!PlatformExtensions.HasPlistValue("NSBluetoothPeripheralUsageDescription"))
                    this.logger.LogCritical("NSBluetoothPeripheralUsageDescription needs to be set - you will likely experience a native crash after this log");

                var background = services.GetService(typeof(IBleDelegate)) != null;
                if (!background)
                    return new CBCentralManager(this, null);

                if (!PlatformExtensions.HasPlistValue("NSBluetoothAlwaysUsageDescription", 13))
                    this.logger.LogCritical("NSBluetoothAlwaysUsageDescription needs to be set - you will likely experience a native crash after this log");

                var opts = new CBCentralInitOptions
                {
                    ShowPowerAlert = config.iOSShowPowerAlert,
                    RestoreIdentifier = config.iOSRestoreIdentifier ?? "shinyble"
                };

                return new CBCentralManager(this, null, opts);
            });
        }


        public IServiceProvider Services { get; }
        public CBCentralManager Manager => this.managerLazy.Value;
        public bool HasRegisteredDelegates => this.Services.GetService(typeof(BleDelegate)) != null;


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
            //this.Manager = central;
            var peripheralArray = (NSArray)dict[CBCentralManager.RestoredStatePeripheralsKey];
            for (nuint i = 0; i < peripheralArray.Count; i++)
            {
                var item = peripheralArray.GetItem<CBPeripheral>(i);
                var peripheral = this.GetPeripheral(item);
                await this.Services.RunDelegates<IBleDelegate>(x => x.OnConnected(peripheral));
            }
            // TODO: restore scan? CBCentralManager.RestoredStateScanOptionsKey
#endif
        }


        public Subject<CBPeripheral> PeripheralConnected { get; } = new Subject<CBPeripheral>();
        public override async void ConnectedPeripheral(CBCentralManager central, CBPeripheral peripheral)
        {
            var p = this.GetPeripheral(peripheral);
            await this.Services.RunDelegates<IBleDelegate>(x => x.OnConnected(p));
            this.PeripheralConnected.OnNext(peripheral);
        }


        public Subject<CBPeripheral> PeripheralDisconnected { get; } = new Subject<CBPeripheral>();
        public override void DisconnectedPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error) => this.PeripheralDisconnected.OnNext(peripheral);


        public Subject<ScanResult> ScanResultReceived { get; } = new Subject<ScanResult>();
        public override void DiscoveredPeripheral(CBCentralManager central, CBPeripheral peripheral, NSDictionary advertisementData, NSNumber rssi)
        {
            var result = new ScanResult(
                this.GetPeripheral(peripheral),
                rssi?.Int32Value ?? 0,
                new AdvertisementData(advertisementData)
            );
            this.ScanResultReceived.OnNext(result);
        }


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
            await this.Services.RunDelegates<IBleDelegate>(x => x.OnAdapterStateChanged(state));
        }
    }
}