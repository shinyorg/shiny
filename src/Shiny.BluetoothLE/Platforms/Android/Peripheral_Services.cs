using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using Android.Bluetooth;
using Microsoft.Extensions.Logging;

namespace Shiny.BluetoothLE;


public partial class Peripheral
{
    public IObservable<BleServiceInfo> GetService(string serviceUuid) => this.GetNativeService(serviceUuid).Select(this.FromNative);

    public IObservable<IReadOnlyList<BleServiceInfo>> GetServices()
    {
        // we want a fresh discovery everytime the user calls this
        // internal functions call GetNativeServices which doesn't force a refresh
        this.RequiresServiceDiscovery = true;
        return this
            .GetNativeServices()
            .Select(x => x.Select(this.FromNative).ToList());
    }
    

    Subject<Unit>? serviceDiscoverySubj;
    public override void OnServicesDiscovered(BluetoothGatt? gatt, GattStatus status)
        => this.serviceDiscoverySubj?.OnNext(Unit.Default);


    protected IObservable<IReadOnlyList<BluetoothGattService>> GetNativeServices() =>
        Observable.FromAsync<IReadOnlyList<BluetoothGattService>>(async ct =>
        {
            this.AssertConnection();

            this.serviceDiscoverySubj ??= new();
            if (!this.RequiresServiceDiscovery && this.Gatt?.Services != null)
                return this.Gatt!.Services!.ToList();

            // TODO: should lock here
            this.RefreshServices(); // force refresh of services on GATT
            var task = this.serviceDiscoverySubj.Take(1).ToTask(ct);
            if (!this.Gatt!.DiscoverServices())
                throw new InvalidOperationException("Android GATT reported that it could not run service discovery");

            await task.ConfigureAwait(false);
            this.RequiresServiceDiscovery = false;

            return this.Gatt.Services!.ToList();
        });


    protected IObservable<BluetoothGattService> GetNativeService(string serviceUuid) => this
        .GetNativeServices()
        .Select(x =>
        {
            var native = this.Gatt!.GetService(Utils.ToUuidType(serviceUuid));
            if (native == null)
                throw new InvalidOperationException("No service found with uuid: " + serviceUuid);

            return native;
        });


    public bool RequiresServiceDiscovery { get; private set; } = true;

    public void RefreshServices()
    {
        this.AssertConnection();
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
                this.logger.LogInformation("Clear Internal Cache Refresh Result: " + result);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "Failed to clear internal device cache");
        }
    }


    Subject<Unit>? servChangeSubj;
    public IObservable<Unit> WhenServicesChanged() => this.servChangeSubj ??= new();
    public override void OnServiceChanged(BluetoothGatt gatt)
        => this.servChangeSubj?.OnNext(Unit.Default);


    protected BleServiceInfo FromNative(BluetoothGattService service) => new(service.Uuid.ToString());
}