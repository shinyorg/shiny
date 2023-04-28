using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Android.Bluetooth;
using Java.IO;
using Microsoft.Extensions.Logging;

namespace Shiny.BluetoothLE;


public partial class Peripheral
{
    public IObservable<BleServiceInfo> GetService(string serviceUuid) => this.GetNativeService(serviceUuid).Select(this.FromNative);

    public IObservable<IReadOnlyList<BleServiceInfo>> GetServices(bool refreshServices) => this
        .GetNativeServices(refreshServices)
        .Select(x => x.Select(this.FromNative).ToList());
    

    readonly Subject<Unit> serviceDiscoverySubj = new();
    public override void OnServicesDiscovered(BluetoothGatt? gatt, GattStatus status)
        => this.serviceDiscoverySubj.OnNext(Unit.Default);


    protected IObservable<IReadOnlyList<BluetoothGattService>> GetNativeServices(bool refreshServices) =>
        Observable.FromAsync<IReadOnlyList<BluetoothGattService>>(async ct =>
        {
            this.AssertConnection();
            if (refreshServices)
                this.RefreshServices();

            if ((this.Gatt!.Services?.Count ?? 0) > 0 && !refreshServices)
                return this.Gatt.Services!.ToList();

            var task = this.serviceDiscoverySubj.Take(1).ToTask(ct);
            if (!this.Gatt.DiscoverServices())
                throw new InvalidOperationException("Android GATT reported that it could not run service discovery");

            await task.ConfigureAwait(false);
            return this.Gatt.Services!.ToList();
        });


    protected IObservable<BluetoothGattService> GetNativeService(string serviceUuid) => this
        .GetNativeServices(false)
        .Select(x =>
        {
            var native = this.Gatt!.GetService(Utils.ToUuidType(serviceUuid));
            if (native == null)
                throw new InvalidOperationException("No service found with uuid: " + serviceUuid);

            return native;
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


    protected BleServiceInfo FromNative(BluetoothGattService service) => new(service.Uuid.ToString());
}