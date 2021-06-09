using System;
using System.Linq;
using System.Reactive.Linq;
using System.Collections.Generic;
using Shiny.BluetoothLE.Internals;
using Android;
using Android.Bluetooth;


namespace Shiny.BluetoothLE
{
    public class BleManager : AbstractBleManager,
                              ICanControlAdapterState,
                              ICanSeePairedPeripherals
    {
        public const string BroadcastReceiverName = "com.shiny.bluetoothle.ShinyBleCentralBroadcastReceiver";
        readonly ManagerContext context;


        public BleManager(ManagerContext context) => this.context = context;


        bool isScanning;
        public override bool IsScanning => this.isScanning;


        public override IObservable<IPeripheral?> GetKnownPeripheral(string peripheralUuid)
        {
            var address = Guid
                .Parse(peripheralUuid)
                .ToByteArray()
                .Skip(10)
                .Take(6)
                .ToArray();

            var native = this.context.Manager.Adapter.GetRemoteDevice(address);
            if (native == null)
                return Observable.Return<IPeripheral?>(null);

            var peripheral = this.context.GetDevice(native);
            return Observable.Return(peripheral);
        }


        public override IObservable<IEnumerable<IPeripheral>> GetConnectedPeripherals(string? serviceUuid = null)
            => Observable.Return(this.context
                .Manager
                .GetConnectedDevices(ProfileType.Gatt)
                .Select(this.context.GetDevice)
            );


        public override IObservable<AccessState> RequestAccess() => Observable.FromAsync(async () =>
        {
            if (!this.context.Android.IsInManifest(Manifest.Permission.Bluetooth))
                return AccessState.NotSetup;

            if (!this.context.Android.IsInManifest(Manifest.Permission.BluetoothAdmin))
                return AccessState.NotSetup;

            var forBackground = this.context.Services.GetService(typeof(IBleDelegate)) != null;
            var result = await this.context.Android.RequestAccess(Manifest.Permission.AccessFineLocation);
            if (result != AccessState.Available)
                return result;

            return this.Status;
        });


        public override AccessState Status => this.context.Status;
        public override IObservable<AccessState> WhenStatusChanged() => this.context
            .StatusChanged()
            .StartWith(this.Status);


        public override IObservable<ScanResult> Scan(ScanConfig? config = null)
        {
            if (this.IsScanning)
                throw new ArgumentException("There is already an active scan");

            return this
                .RequestAccess()
                .Do(access =>
                {
                    if (access != AccessState.Available)
                        throw new PermissionException("BluetoothLE", access);

                    this.IsScanning = true;
                })
                .SelectMany(_ => this.context.Scan(config ?? new ScanConfig()))
                .Finally(() => this.IsScanning = false);
        }


        public override void StopScan()
        {
            this.context.StopScan();
            this.isScanning = false;
        }


        public IObservable<IEnumerable<IPeripheral>> GetPairedPeripherals() => Observable.Return(this.context
            .Manager
            .Adapter
            .BondedDevices
            .Where(x => x.Type == BluetoothDeviceType.Dual || x.Type == BluetoothDeviceType.Le)
            .Select(this.context.GetDevice)
        );


        public IObservable<bool> SetAdapterState(bool enable)
        {
            var result = false;
            if (enable && !BluetoothAdapter.DefaultAdapter.IsEnabled)
                result = BluetoothAdapter.DefaultAdapter.Enable();

            else if (!enable && BluetoothAdapter.DefaultAdapter.IsEnabled)
                result = BluetoothAdapter.DefaultAdapter.Disable();

            return Observable.Return(result);
        }
    }
}