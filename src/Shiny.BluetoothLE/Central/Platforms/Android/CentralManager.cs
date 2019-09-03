using System;
using System.Linq;
using System.Reactive.Linq;
using System.Collections.Generic;
using Shiny.BluetoothLE.Central.Internals;
using Android;
using Android.Bluetooth;
using Android.Content;
using Android.OS;


namespace Shiny.BluetoothLE.Central
{
    public class CentralManager : AbstractCentralManager,
                                  ICanControlAdapterState,
                                  ICanOpenAdapterSettings,
                                  ICanSeePairedPeripherals
    {
        readonly CentralContext context;
        readonly IMessageBus messageBus;
        bool isScanning;


        public CentralManager(AndroidContext context, IMessageBus messageBus)
        {
            this.context = new CentralContext(context);
            this.messageBus = messageBus;
        }


        public override string AdapterName => "Default Bluetooth Peripheral";
        //public override BleFeatures Features => BleFeatures.ControlAdapterState |
        //                                        BleFeatures.LowPoweredScan |
        //                                        BleFeatures.MtuRequests |
        //                                        BleFeatures.OpenSettings |
        //                                        BleFeatures.PairingRequests |
        //                                        BleFeatures.ReliableTransactions |
        //                                        BleFeatures.ViewPairedPeripherals;
        public override bool IsScanning => this.isScanning;


        public override IObservable<IPeripheral> GetKnownPeripheral(Guid deviceId)
        {
            var native = this.context.Manager.Adapter.GetRemoteDevice(deviceId
                .ToByteArray()
                .Skip(10)
                .Take(6)
                .ToArray()
            );
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


        public override AccessState Status
        {
            get
            {
                if (Build.VERSION.SdkInt < BuildVersionCodes.Lollipop)
                    return AccessState.NotSupported;

                if (this.context.Manager?.Adapter == null)
                    return AccessState.NotSupported;

                if (!this.context.Manager.Adapter.IsEnabled)
                    return AccessState.Disabled;

                return this.context.Manager.Adapter.State.FromNative();
            }
        }


        public override IObservable<AccessState> WhenStatusChanged() => this.messageBus
            .Listener<AccessState>() // TODO: needs to be more defined
            //.Select(x => this.Status)
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