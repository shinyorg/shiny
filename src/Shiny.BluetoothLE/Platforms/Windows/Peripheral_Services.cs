using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using Windows.Devices.Bluetooth;

namespace Shiny.BluetoothLE;


public partial class Peripheral : IPeripheral
{
    public IObservable<Unit> WhenServicesChanged() => null;
    public IObservable<BleServiceInfo> GetService(string serviceUuid) => throw new NotImplementedException();
    public IObservable<IReadOnlyList<BleServiceInfo>> GetServices() => Observable.FromAsync(async ct =>
    {
        var result = await this.Native!
            .GetGattServicesAsync(BluetoothCacheMode.Uncached)
            .AsTask(ct)
            .ConfigureAwait(false);

        // TODO
        //result.Status.Assert();
        return result
            .Services
            .Select(x => new BleServiceInfo(x.Uuid.ToString()))
            .ToList();
    });

}