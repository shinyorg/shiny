using System;
using System.Linq;
using System.Reactive.Linq;
using System.Collections.Generic;
using Shiny.BluetoothLE.Central.Internals;
using Android;
using Android.Bluetooth;
using Android.Content;


namespace Shiny.BluetoothLE.Central
{
    public class CentralManager : AbstractCentralManager,
                                  ICanControlAdapterState,
                                  ICanOpenAdapterSettings,
                                  ICanSeePairedPeripherals
    {
        public const string BroadcastReceiverName = "com.shiny.bluetoothle.ShinyBleCentralBroadcastReceiver";

        readonly CentralContext context;
        bool isScanning;


        public CentralManager(CentralContext context)
        {
            this.context = context;
        }


        public override string AdapterName => "Default Bluetooth Peripheral";
        public override bool IsScanning => this.isScanning;


        public override IObservable<IPeripheral> GetKnownPeripheral(Guid deviceId)
        {
            var native = this.context.Manager.Adapter.GetRemoteDevice(deviceId
                .ToByteArray()
                .Skip(10)
                .Take(6)
                .ToArray()
            );
            if (native == null)
                return Observable.Return<IPeripheral>(null);

            var device = this.context.GetDevice(native);
            return Observable.Return(device);
        }


        public override IObservable<IEnumerable<IPeripheral>> GetConnectedPeripherals(Guid? serviceUuid = null)
            => Observable.Return(this.context
                .Manager
                .GetConnectedDevices(ProfileType.Gatt)
                .Select(this.context.GetDevice));


        public override IObservable<AccessState> RequestAccess() => Observable.FromAsync(async () =>
        {
            var result = await this.context.Android.RequestAccess(Manifest.Permission.AccessCoarseLocation);
            if (result != AccessState.Available)
                return result;

            return this.Status;
        });


        public override AccessState Status => this.context.Status;
        public override IObservable<AccessState> WhenStatusChanged() => this.context
            .StatusChanged
            .StartWith(this.Status);


        public override IObservable<IScanResult> Scan(ScanConfig config)
        {
            // TODO: check permissions first
            if (this.IsScanning)
                throw new ArgumentException("There is already an active scan");

            this.isScanning = true;
            return this.context
                .Scan(config ?? new ScanConfig())
                .Finally(() => this.isScanning = false);
        }


        public override void StopScan()
        {
            if (!this.IsScanning)
                return;

            this.isScanning = false;
            this.context.StopScan();
        }


        public IObservable<IEnumerable<IPeripheral>> GetPairedPeripherals()
            => Observable.Return(this.context
                .Manager
                .Adapter
                .BondedDevices
                .Where(x => x.Type == BluetoothDeviceType.Dual || x.Type == BluetoothDeviceType.Le)
                .Select(this.context.GetDevice));


        public bool OpenSettings()
        {
            var intent = new Intent(Android.Provider.Settings.ActionBluetoothSettings);
            intent.SetFlags(ActivityFlags.NewTask);
            this.context.Android.AppContext.StartActivity(intent);
            return true;
        }


        public void SetAdapterState(bool enable)
        {
            if (enable && !BluetoothAdapter.DefaultAdapter.IsEnabled)
                BluetoothAdapter.DefaultAdapter.Enable();

            else if (!enable && BluetoothAdapter.DefaultAdapter.IsEnabled)
                BluetoothAdapter.DefaultAdapter.Disable();
        }
    }
}