using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Android.Bluetooth;
using Microsoft.Extensions.Logging;

namespace Shiny.BluetoothLE;


public partial class Peripheral
{
    public IObservable<BleServiceInfo> GetService(string serviceUuid) => this.GetNativeService(serviceUuid).Select(this.FromNative);

    public IObservable<IReadOnlyList<BleServiceInfo>> GetServices(bool refreshServices) => Observable.FromAsync<IReadOnlyList<BleServiceInfo>>(async ct =>
    {
        this.AssertConnection();

        if (refreshServices)
            this.RefreshServices();

        if (this.Gatt!.Services != null && !refreshServices)
            return this.Gatt.Services.Select(this.FromNative).ToList();

        var task = this.serviceDiscoverySubj.Take(1).ToTask(ct);
        this.Gatt.DiscoverServices();

        await task.ConfigureAwait(false);
        var services = this.Gatt
            .Services!
            .Select(x => new BleServiceInfo(
                x.Uuid!.ToString()
            ))
            .ToList();

        return services;
    });


    readonly Subject<Unit> serviceDiscoverySubj = new();
    public override void OnServicesDiscovered(BluetoothGatt? gatt, GattStatus status)
        => this.serviceDiscoverySubj.OnNext(Unit.Default);


    protected IObservable<BluetoothGattService> GetNativeService(string serviceUuid) => Observable.Create<BluetoothGattService>(ob =>
    {
        this.AssertConnection();

        var service = this.Gatt!.GetService(Utils.ToUuidType(serviceUuid));
        if (service == null)
            throw new InvalidOperationException("No service found with uuid: " + serviceUuid);

        ob.Respond(service);
        return Disposable.Empty;
    });


    protected void RefreshServices()
    {
        // https://stackoverflow.com/questions/22596951/how-to-programmatically-force-bluetooth-low-energy-service-discovery-on-android
        try
        {
            var method = this.Gatt!.Class.GetMethod("refresh");
            if (method == null)
            {
                this.logger.LogWarning("No internal refresh method found");
            }
            else
            {
                var result = (bool)method.Invoke(this.Gatt);
                this.logger.LogWarning("Clear Internal Cache Refresh Result: " + result);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to clear internal device cache");
        }
    }


    protected BleServiceInfo FromNative(BluetoothGattService service) => new BleServiceInfo(service.Uuid.ToString());
}