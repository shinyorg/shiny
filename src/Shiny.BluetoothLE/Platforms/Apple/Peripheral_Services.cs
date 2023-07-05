using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using CoreBluetooth;
using Foundation;
using Microsoft.Extensions.Logging;
using Shiny.BluetoothLE.Intrastructure;

namespace Shiny.BluetoothLE;


public partial class Peripheral
{
    protected IObservable<CBService> GetNativeService(string serviceUuid) => this.operations.QueueToObservable(async ct =>
    {
        this.AssertConnnection();

        var nativeUuid = CBUUID.FromString(serviceUuid);
        var service = this.TryGetService(nativeUuid);

        if (service != null)
            return service;

        this.serviceDiscoverySubj ??= new();
        var task = this.serviceDiscoverySubj.Take(1).ToTask(ct);

        this.Native.DiscoverServices(new[] { nativeUuid });
        var result = await task.ConfigureAwait(false);
        if (result != null)
            throw new InvalidOperationException(result.LocalizedDescription);

        // the service should be scope now
        service = this.TryGetService(nativeUuid);
        if (service == null)
            throw new InvalidOperationException("No service found for " + serviceUuid);

        return service;
    });


    public IObservable<BleServiceInfo> GetService(string serviceUuid) => this
        .GetNativeService(serviceUuid)
        .Select(this.FromNative);


    public IObservable<IReadOnlyList<BleServiceInfo>> GetServices() => Observable.Create<IReadOnlyList<BleServiceInfo>>(ob =>
    {
        this.AssertConnnection();

        this.serviceDiscoverySubj ??= new();
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


    Subject<NSError?>? serviceDiscoverySubj;
    public override void DiscoveredService(CBPeripheral peripheral, NSError? error)
    {
        this.logger.ServiceDiscoveryEvent(peripheral.Identifier, peripheral.Services?.Length ?? 0);
        this.serviceDiscoverySubj?.OnNext(error);
    }


    Subject<Unit> servChangeSubj;
    public IObservable<Unit> WhenServicesChanged() => this.servChangeSubj ??= new();
    public override void ModifiedServices(CBPeripheral peripheral, CBService[] services)
    {
        if (this.logger.IsEnabled(LogLevel.Debug))
            this.logger.LogDebug($"[ModifiedServices] Peripheral: {peripheral.Identifier} - Services: {services.Length}");

        this.servChangeSubj?.OnNext(Unit.Default);
    }
}