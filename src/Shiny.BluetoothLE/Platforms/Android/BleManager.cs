using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Collections.Generic;
using Shiny.BluetoothLE.Internals;
using Android;
using Android.Bluetooth;

namespace Shiny.BluetoothLE;


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
            .Where(x => x.Type == BluetoothDeviceType.Dual || x.Type == BluetoothDeviceType.Le) // just in case
            .Select(this.context.GetDevice)
        );


    public override IObservable<AccessState> RequestAccess(bool connect = true) => Observable.FromAsync(async ct =>
    {
        var list = new List<string>(new[]
        {
            Manifest.Permission.Bluetooth,
            Manifest.Permission.BluetoothAdmin
        });

        if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            list.Add(Manifest.Permission.BluetoothScan);

            if (connect)
                list.Add(Manifest.Permission.BluetoothConnect);
        }
        else
        {
            list.Add(Manifest.Permission.AccessFineLocation);
        }

        if (!list.All(x => this.context.Android.IsInManifest(x)))
            return AccessState.NotSetup;

        var results = await this.context
            .Android
            .RequestPermissions(list.ToArray())
            .ToTask(ct)
            .ConfigureAwait(false);

        return results.IsSuccess()
            ? this.context.Status // now look at the actual device state
            : AccessState.Denied;
    });


    public override IObservable<ScanResult> Scan(ScanConfig? config = null)
    {
        if (this.IsScanning)
            throw new InvalidOperationException("There is already an active scan");

        return this
            .RequestAccess(false)
            .Do(access =>
            {
                if (access != AccessState.Available)
                    throw new InvalidOperationException($"Invalid Status: {access} - Ensure you have the following permissions in your AndroidManifest.xml - android.permission.ACCESS_FINE_LOCATION, android.permission.BLUETOOTH_SCAN, & android.permission.BLUETOOTH_CONNECT");

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
        var ad = this.context.Manager.Adapter!;
        if (enable && !ad.IsEnabled)
            result =ad.Enable();

        else if (!enable && ad.IsEnabled)
            result = ad.Disable();

        return Observable.Return(result);
    }
}