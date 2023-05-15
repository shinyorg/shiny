using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using CoreBluetooth;
using Foundation;

namespace Shiny.BluetoothLE;


public partial class Peripheral
{
    protected IObservable<CBService> GetNativeService(string serviceUuid) => Observable.Create<CBService>(ob =>
    {
        this.AssertConnnection();

        IDisposable? disp = null;
        var nativeUuid = CBUUID.FromString(serviceUuid);
        var service = this.TryGetService(nativeUuid);

        if (service != null)
        {
            ob.Respond(service);
        }
        else
        {
            disp = this.serviceDiscoverySubj.Subscribe(x =>
            {
                if (x != null)
                {
                    ob.OnError(new InvalidOperationException(x.LocalizedDescription));
                }
                else
                {
                    // the service should be scope now
                    service = this.TryGetService(nativeUuid);
                    if (service == null)
                        ob.OnError(new InvalidOperationException("No service found for " + serviceUuid));
                    else
                        ob.Respond(service);
                }
            });
            this.Native.DiscoverServices(new[] { nativeUuid });
        }
        return () => disp?.Dispose();
    });


    public IObservable<BleServiceInfo> GetService(string serviceUuid) => this
        .GetNativeService(serviceUuid)
        .Select(this.FromNative);


    public IObservable<IReadOnlyList<BleServiceInfo>> GetServices() => Observable.Create<IReadOnlyList<BleServiceInfo>>(ob =>
    {
        this.AssertConnnection();

        var disp = this.serviceDiscoverySubj.Subscribe(x =>
        {
            if (x != null)
            {
                ob.OnError(new InvalidOperationException(x.LocalizedDescription));
            }
            else if (this.Native.Services != null)
            {
                ob.Respond(this.Native.Services.Select(this.FromNative).ToList());
            }
        });
        this.Native.DiscoverServices();

        return () => disp?.Dispose();
    });


    protected BleServiceInfo FromNative(CBService native) => new(
        native.UUID.ToString()
    );


    CBService? TryGetService(CBUUID nativeUuid)
    {
        var service = this.Native.Services?.FirstOrDefault(x => x.UUID.Equals(nativeUuid));
        return service;
    }


    readonly Subject<NSError?> serviceDiscoverySubj = new();
    public override void DiscoveredService(CBPeripheral peripheral, NSError? error)
    {
        Log.ServiceDiscoveryEvent(this.logger, peripheral.Identifier, peripheral.Services?.Length ?? 0);
        this.serviceDiscoverySubj.OnNext(error);
    }
}