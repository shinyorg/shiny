using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Collections.Generic;
using Shiny.BluetoothLE.Internals;
using Android;
using Android.Bluetooth;
using P = Android.Manifest.Permission;


namespace Shiny.BluetoothLE
{
    public class BleManager : AbstractBleManager,
                              ICanControlAdapterState,
                              ICanViewPairedPeripherals
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

            var native = this.context.Manager.Adapter!.GetRemoteDevice(address);
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


        public override IObservable<AccessState> RequestAccess() => Observable.FromAsync(async ct =>
        {
            if (!this.context.Android.IsInManifest(Manifest.Permission.Bluetooth))
                return AccessState.NotSetup;

            if (!this.context.Android.IsInManifest(Manifest.Permission.BluetoothAdmin))
                return AccessState.NotSetup;

            var permissions = new List<string>() { P.AccessFineLocation };
            if (this.context.Android.IsMinApiLevel(31))
            {
                permissions.Add("android.permission.BLUETOOTH_SCAN");    //Required to be able to discover and pair nearby Bluetooth devices
                permissions.Add("android.permission.BLUETOOTH_CONNECT"); //Required to be able to connect to paired Bluetooth devices
            }
            var results = await this.context
                .Android
                .RequestPermissions(permissions.ToArray())
                .ToTask(ct)
                .ConfigureAwait(false);

            return results.IsSuccess()
                ? this.context.Status // now look at the actual device state
                : AccessState.Denied;

    //< uses - permission android: name = "android.permission.BLUETOOTH"
    //                 android: maxSdkVersion = "30" />

// < uses - permission android: name = "android.permission.BLUETOOTH_ADMIN"
//                 android: maxSdkVersion = "30" />
            // 31
//< uses - permission android: name = "android.permission.BLUETOOTH_SCAN"
//                     android: usesPermissionFlags = "neverForLocation" />
        });


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
            .Adapter!
            .BondedDevices
            .Where(x => x.Type == BluetoothDeviceType.Dual || x.Type == BluetoothDeviceType.Le)
            .Select(this.context.GetDevice)
        );


        public IObservable<bool> SetAdapterState(bool enable)
        {
            var result = false;
            if (BluetoothAdapter.DefaultAdapter != null)
            {
                if (enable && !BluetoothAdapter.DefaultAdapter.IsEnabled)
                    result = BluetoothAdapter.DefaultAdapter.Enable();

                else if (!enable && BluetoothAdapter.DefaultAdapter.IsEnabled)
                    result = BluetoothAdapter.DefaultAdapter.Disable();
            }
            return Observable.Return(result);
        }
    }
}