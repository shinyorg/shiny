using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.GenericAttributeProfile;
using Shiny.BluetoothLE.Infrastructure;

namespace Shiny.BluetoothLE;


public partial class Peripheral
{
    public IObservable<BleServiceInfo> GetService(string serviceUuid) => ObservableTask.FromAsync(async ct =>
    {
        this.AssertConnection();

        var service = await this.GetNativeServiceAsync(serviceUuid, ct).ConfigureAwait(false);
        return new BleServiceInfo(service.Uuid.ToString());
    });


    public IObservable<IReadOnlyList<BleServiceInfo>> GetServices() => ObservableTask.FromAsync(async ct =>
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
