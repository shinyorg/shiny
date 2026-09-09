using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Windows.Devices.Bluetooth;
using Windows.Devices.Bluetooth.Advertisement;
using System.Runtime.InteropServices.WindowsRuntime;

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
        this.ResetAdvertisement();

        if (!options.LocalName.IsEmpty())
            this.publisher.Advertisement.LocalName = options.LocalName!;

        this.publisher.Advertisement.Flags = BluetoothLEAdvertisementFlags.ClassicNotSupported;

        foreach (var serviceUuid in options.ServiceUuids)
        {
            var uuid = UuidHelper.ToUuid(serviceUuid);
            this.publisher.Advertisement.ServiceUuids.Add(uuid);
        }

        foreach (var sd in options.ServiceData)
            this.publisher.Advertisement.DataSections.Add(ToServiceDataSection(sd));

        if (options.ManufacturerData != null)
        {
            this.publisher.Advertisement.ManufacturerData.Add(new BluetoothLEManufacturerData(
                options.ManufacturerData.CompanyId,
                options.ManufacturerData.Data.AsBuffer()
            ));
        }

        this.publisher.Start();
        return Task.CompletedTask;
    }


    public void StopAdvertising() => this.publisher.Stop();


    public Task AdvertiseBeacon(Guid uuid, ushort major, ushort minor, sbyte? txpower = null)
    {
        // Windows does support this - it just needs the payload put together by hand, because there
        // is no iBeacon-shaped API the way CoreBluetooth has one. The manufacturer data section is
        // exactly what an iBeacon is on the wire.
        var payload = IBeaconPacket.Build(uuid, major, minor, txpower ?? IBeaconPacket.DefaultTxPower);

        this.ResetAdvertisement();
        this.publisher.Advertisement.ManufacturerData.Add(new BluetoothLEManufacturerData(
            IBeaconPacket.AppleCompanyId,
            payload.AsBuffer()
        ));
        this.publisher.Start();

        return Task.CompletedTask;
    }


    void ResetAdvertisement()
    {
        this.publisher.Advertisement.LocalName = String.Empty;
        this.publisher.Advertisement.ManufacturerData.Clear();
        this.publisher.Advertisement.ServiceUuids.Clear();
        this.publisher.Advertisement.DataSections.Clear();
    }


    /// <summary>
    /// Packs a service data entry into a raw AD structure. WinRT has no service data collection the
    /// way it has one for manufacturer data, so the 16-bit UUID and its payload are laid out by hand
    /// - UUID first, little-endian, as the Bluetooth core specification requires.
    /// </summary>
    static BluetoothLEAdvertisementDataSection ToServiceDataSection(AdvertisementServiceData serviceData)
    {
        var uuid = UuidHelper.ToUuid(serviceData.Uuid);
        var shortId = UuidHelper.To16BitUuid(uuid);

        if (shortId == null)
            throw new NotSupportedException($"Windows service data advertising supports 16-bit service UUIDs only - '{serviceData.Uuid}' is not one");

        var bytes = new byte[2 + serviceData.Data.Length];
        bytes[0] = (byte)(shortId.Value & 0xFF);
        bytes[1] = (byte)(shortId.Value >> 8);
        serviceData.Data.CopyTo(bytes, 2);

        return new BluetoothLEAdvertisementDataSection(
            BluetoothLEAdvertisementDataTypes.ServiceData16BitUuids,
            bytes.AsBuffer()
        );
    }


    public Task<L2CapInstance> OpenL2Cap(bool secure, Action<L2CapChannel> onOpen)
        => throw new NotSupportedException("L2CAP hosting is not supported on Windows");
}
