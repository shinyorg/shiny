using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;

namespace Shiny.BluetoothLE;


public partial class Peripheral
{
    public IObservable<BleServiceInfo> GetService(string serviceUuid) => Observable.FromAsync(async ct =>
    {
        this.AssertConnection();

        var suid = Utils.ToUuidType(serviceUuid);
        var result = await this.Native!
            .GetGattServicesForUuidAsync(suid, BluetoothCacheMode.Cached)
            .AsTask(ct)
            .ConfigureAwait(false);

        result.Status.Assert("GetService", serviceUuid);

        var service = result.Services.FirstOrDefault();
        if (service == null)
            throw new BleException($"Service '{serviceUuid}' not found");

        this.TrackService(service);
        return new BleServiceInfo(service.Uuid.ToString());
    });


    public IObservable<IReadOnlyList<BleServiceInfo>> GetServices() => Observable.FromAsync(async ct =>
    {
        this.AssertConnection();

        var result = await this.Native!
            .GetGattServicesAsync(BluetoothCacheMode.Uncached)
            .AsTask(ct)
            .ConfigureAwait(false);

        result.Status.Assert("GetServices", "all");

        foreach (var service in result.Services)
            this.TrackService(service);

        return (IReadOnlyList<BleServiceInfo>)result
            .Services
            .Select(x => new BleServiceInfo(x.Uuid.ToString()))
            .ToList();
    });
}
