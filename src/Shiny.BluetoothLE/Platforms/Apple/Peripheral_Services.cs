using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using CoreBluetooth;
using Foundation;

namespace Shiny.BluetoothLE;


public partial class Peripheral
{
    public IObservable<BleServiceInfo> GetService(string serviceUuid) => Observable.Create<BleServiceInfo>(ob =>
    {
        var nativeUuid = CBUUID.FromString(serviceUuid);
        var service = this.TryGetService(nativeUuid);
        if (service != null)
        {
            ob.Respond(FromNative(service));
            return Disposable.Empty;
        }
        var handler = new EventHandler<NSErrorEventArgs>((sender, args) =>
        {
            if (this.Native.Services == null)
                return;

            var service = this.TryGetService(nativeUuid);
            if (service == null)
                ob.OnError(new ArgumentException("This service does not exist"));
            else
                ob.Respond(FromNative(service));
        });

        this.Native.DiscoveredService += handler;
        this.Native.DiscoverServices(new[] { nativeUuid });

        return Disposable.Create(() => this.Native.DiscoveredService -= handler);
    });


    public IObservable<IReadOnlyList<BleServiceInfo>> GetServices() => Observable.Create<IReadOnlyList<BleServiceInfo>>(ob =>
    {
        var handler = new EventHandler<NSErrorEventArgs>((sender, args) =>
        {
            if (args.Error != null)
            {
                ob.OnError(new BleException(args.Error.LocalizedDescription));
                return;
            }

            if (this.Native.Services != null)
            {
                var list = this.Native
                    .Services
                    .Select(native => new BleServiceInfo(native.UUID.ToString()))
                    .ToList();

                ob.Respond(list);
            }
        });
        this.Native.DiscoveredService += handler;
        this.Native.DiscoverServices();

        return () => this.Native.DiscoveredService -= handler;
    });


    static BleServiceInfo FromNative(CBService native) => new BleServiceInfo(
        native.UUID.ToString()
    );


    CBService? TryGetService(CBUUID nativeUuid)
    {
        var service = this.Native.Services?.FirstOrDefault(x => x.UUID.Equals(nativeUuid));
        return service;
    }
}