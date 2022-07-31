using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using CoreBluetooth;
using Foundation;
using Shiny.BluetoothLE.Internals;

namespace Shiny.BluetoothLE;


public class BleManager : AbstractBleManager
{
    const string ErrorCategory = "BluetoothLE";
    readonly ManagerContext context;


    public BleManager(ManagerContext context) => this.context = context;


    public override bool IsScanning => this.context.Manager.IsScanning;


    public override IObservable<AccessState> RequestAccess(bool connect = true) => Observable.Create<AccessState>(ob =>
    {
        IDisposable? disp = null;
        if (this.context.Manager.State == CBManagerState.Unknown)
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


    public override IObservable<IPeripheral?> GetKnownPeripheral(string peripheralUuid)
    {
        var uuid = new NSUuid(peripheralUuid);
        var peripheral = this.context
            .Manager
            .RetrievePeripheralsWithIdentifiers(uuid)
            .FirstOrDefault();

        if (peripheral == null)
            return Observable.Return<IPeripheral?>(null);

        var device = this.context.GetPeripheral(peripheral);
        return Observable.Return(device);
    }


    public override IObservable<IEnumerable<IPeripheral>> GetConnectedPeripherals(string? serviceUuid = null)
    {
        if (serviceUuid == null)
            return Observable.Return(this.context.GetConnectedDevices().ToList());

        return Observable.Return(this
            .context
            .Manager
            .RetrieveConnectedPeripherals(CBUUID.FromString(serviceUuid))
            .Select(x => this.context.GetPeripheral(x))
            .ToList()
        );
    }


    static readonly PeripheralScanningOptions PeripheralScanningOptions = new PeripheralScanningOptions { AllowDuplicatesKey = true };

    public override IObservable<ScanResult> Scan(ScanConfig? config = null)
    {
        if (this.IsScanning)
            throw new ArgumentException("There is already an existing scan");

        config ??= new ScanConfig();
        return this.RequestAccess()
            .Do(access =>
            {
                if (access != AccessState.Available)
                    throw new PermissionException(ErrorCategory, access);
            })
            .SelectMany(_ =>
            {
                if (config.ServiceUuids == null || config.ServiceUuids.Length == 0)
                {
                    this.context.Manager.ScanForPeripherals(
                        null!,
                        PeripheralScanningOptions
                    );
                }
                else
                {
                    var uuids = config.ServiceUuids.Select(CBUUID.FromString).ToArray();
                    this.context.Manager.ScanForPeripherals(uuids, PeripheralScanningOptions);
                }
                return this.context.ScanResultReceived;
            })
            .Finally(() =>
                this.context.Manager.StopScan()
            );
    }


    public override void StopScan() => this.context.Manager.StopScan();
}