using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;

namespace Shiny.BluetoothLE.Hosting;


public partial class BleHostingManager : IBleHostingManager
{
    readonly BluetoothLEAdvertisementPublisher publisher = new();
    readonly Dictionary<string, GattService> gattSvcs = new();


    public AccessState AdvertisingAccessStatus
    {
        get
        {
            var adapter = BluetoothAdapter.GetDefaultAsync().GetAwaiter().GetResult();
            if (adapter == null || !adapter.IsLowEnergySupported || !adapter.IsPeripheralRoleSupported)
                return AccessState.NotSupported;

            return AccessState.Available;
        }
    }


    public AccessState GattAccessStatus => this.AdvertisingAccessStatus;


    public async Task<AccessState> RequestAccess(bool advertise = true, bool connect = true)
    {
        var adapter = await BluetoothAdapter.GetDefaultAsync();
        if (adapter == null)
            return AccessState.NotSupported;

        if (!adapter.IsLowEnergySupported || !adapter.IsPeripheralRoleSupported)
            return AccessState.NotSupported;

        return AccessState.Available;
    }


    public bool IsAdvertising => this.publisher.Status == BluetoothLEAdvertisementPublisherStatus.Started;

    public IReadOnlyList<IGattService> Services => this.gattSvcs.Values.Cast<IGattService>().ToList();


    public async Task<IGattService> AddService(string uuid, bool primary, Action<IGattServiceBuilder> serviceBuilder)
    {
        var sb = new GattService(uuid, primary);
        serviceBuilder(sb);
        await sb.Build();
        this.gattSvcs.Add(uuid, sb);
        return sb;
    }


    public void ClearServices()
    {
        foreach (var service in this.gattSvcs.Values)
            service.Dispose();

        this.gattSvcs.Clear();
    }


    public void RemoveService(string serviceUuid)
    {
        if (!this.gattSvcs.ContainsKey(serviceUuid))
            return;

        this.gattSvcs[serviceUuid].Dispose();
        this.gattSvcs.Remove(serviceUuid);
    }


    public Task StartAdvertising(AdvertisementOptions? options = null)
    {
        options ??= new AdvertisementOptions();
        if (!options.LocalName.IsEmpty())
            this.publisher.Advertisement.LocalName = options.LocalName!;

        this.publisher.Advertisement.Flags = BluetoothLEAdvertisementFlags.ClassicNotSupported;
        this.publisher.Advertisement.ManufacturerData.Clear();
        this.publisher.Advertisement.ServiceUuids.Clear();

        foreach (var serviceUuid in options.ServiceUuids)
        {
            var uuid = UuidHelper.ToUuid(serviceUuid);
            this.publisher.Advertisement.ServiceUuids.Add(uuid);
        }
        this.publisher.Start();
        return Task.CompletedTask;
    }


    public void StopAdvertising() => this.publisher.Stop();


    public Task AdvertiseBeacon(Guid uuid, ushort major, ushort minor, sbyte? txpower = null)
        => throw new NotSupportedException("iBeacon advertising is not supported on Windows");


    public Task<L2CapInstance> OpenL2Cap(bool secure, Action<L2CapChannel> onOpen)
        => throw new NotSupportedException("L2CAP hosting is not supported on Windows");
}
