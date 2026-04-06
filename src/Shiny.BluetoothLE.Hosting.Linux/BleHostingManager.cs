using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.BluetoothLE.Hosting.Bluez;
using Tmds.DBus.Protocol;

namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// BLE peripheral hosting via BlueZ on Linux. Talks to the system D-Bus and exposes
/// services/characteristics/advertisements through BlueZ's GattManager1 / LEAdvertisingManager1.
/// </summary>
public class BleHostingManager : IBleHostingManager, IAsyncDisposable
{
    readonly ILogger<BleHostingManager> logger;
    readonly Dictionary<string, GattService> services = new();
    Connection? connection;
    bool advertising;


    public BleHostingManager(ILogger<BleHostingManager> logger)
    {
        this.logger = logger;
    }


    public AccessState AdvertisingAccessStatus { get; private set; } = AccessState.Unknown;
    public AccessState GattAccessStatus { get; private set; } = AccessState.Unknown;
    public bool IsAdvertising => this.advertising;
    public bool IsRegisteredServicesAttached { get; private set; }
    public IReadOnlyList<IGattService> Services => this.services.Values.Cast<IGattService>().ToList();


    public async Task<AccessState> RequestAccess(bool advertise = true, bool connect = true)
    {
        try
        {
            await this.EnsureConnectionAsync().ConfigureAwait(false);

            // Probe BlueZ adapter for Powered=true. If the adapter object exists and is reachable,
            // we treat that as "Available". A more thorough probe would call Properties.Get on
            // org.bluez.Adapter1 / Powered.
            var available = await this.IsAdapterAvailableAsync().ConfigureAwait(false)
                ? AccessState.Available
                : AccessState.Disabled;

            if (advertise) this.AdvertisingAccessStatus = available;
            if (connect)   this.GattAccessStatus = available;
            return available;
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "BlueZ not reachable on system D-Bus");
            this.AdvertisingAccessStatus = AccessState.NotSupported;
            this.GattAccessStatus = AccessState.NotSupported;
            return AccessState.NotSupported;
        }
    }


    public async Task<IGattService> AddService(string uuid, bool primary, Action<IGattServiceBuilder> serviceBuilder)
    {
        await this.EnsureConnectionAsync().ConfigureAwait(false);

        var svc = new GattService(uuid, primary);
        serviceBuilder(svc);

        // Assign object paths now so user code (e.g. Notify) can reason about them.
        var index = this.services.Count;
        svc.ObjectPath = $"{BluezConstants.ApplicationRootPath}/service{index}";
        for (var i = 0; i < svc.NativeCharacteristics.Count; i++)
            svc.NativeCharacteristics[i].ObjectPath = $"{svc.ObjectPath}/char{i}";

        this.services.Add(uuid, svc);

        // TODO: export GattService1/GattCharacteristic1 D-Bus objects via Connection.AddMethodHandler
        // and (re)call org.bluez.GattManager1.RegisterApplication on the adapter so BlueZ picks
        // up the application root at BluezConstants.ApplicationRootPath.
        throw new NotSupportedException(
            "GATT server registration with BlueZ is not yet implemented. " +
            "The service has been recorded but not exported over D-Bus."
        );
    }


    public void RemoveService(string serviceUuid)
    {
        if (this.services.Remove(serviceUuid))
        {
            // TODO: unregister application from BlueZ if no services remain, otherwise re-register.
        }
    }


    public void ClearServices()
    {
        this.services.Clear();
        // TODO: call GattManager1.UnregisterApplication
    }


    public Task AttachRegisteredServices()
    {
        // The shared BleHostingManager (in the multi-target package) drives this via reflection on
        // BleGattCharacteristic-derived DI registrations. The Linux variant intentionally does not
        // hook into IShinyStartupTask — call AddService manually until the GATT export path is wired.
        this.IsRegisteredServicesAttached = false;
        return Task.CompletedTask;
    }


    public void DetachRegisteredServices()
    {
        this.IsRegisteredServicesAttached = false;
        this.ClearServices();
    }


    public Task StartAdvertising(AdvertisementOptions? options = null)
    {
        // TODO: export LEAdvertisement1 object at BluezConstants.AdvertisementPath with
        // Type=peripheral, ServiceUUIDs, LocalName from options, then call
        // org.bluez.LEAdvertisingManager1.RegisterAdvertisement on the adapter.
        throw new NotSupportedException("LE advertising via BlueZ is not yet implemented.");
    }


    public void StopAdvertising()
    {
        if (!this.advertising) return;
        // TODO: call LEAdvertisingManager1.UnregisterAdvertisement and remove the exported object.
        this.advertising = false;
    }


    public Task AdvertiseBeacon(Guid uuid, ushort major, ushort minor, sbyte? txpower = null)
        => throw new NotSupportedException("iBeacon advertising is not supported on Linux/BlueZ.");


    async Task EnsureConnectionAsync(CancellationToken ct = default)
    {
        if (this.connection != null) return;
        this.connection = new Connection(Address.System!);
        await this.connection.ConnectAsync().ConfigureAwait(false);
    }


    async Task<bool> IsAdapterAvailableAsync()
    {
        if (this.connection == null)
            return false;

        // Best-effort: ask BlueZ Properties.Get for Adapter1.Powered. If the call succeeds and
        // returns true, the adapter is up. We swallow exceptions so missing/down adapters return
        // false rather than throwing.
        try
        {
            var msg = this.connection.CreateGetPropertyCall(
                BluezConstants.Service,
                BluezConstants.DefaultAdapterPath,
                BluezConstants.AdapterInterface,
                "Powered"
            );

            var powered = await this.connection.CallMethodAsync(
                msg,
                static (Message reply, object? _) =>
                {
                    var reader = reply.GetBodyReader();
                    return reader.ReadBoolVariant();
                }
            ).ConfigureAwait(false);

            return powered;
        }
        catch
        {
            return false;
        }
    }


    public ValueTask DisposeAsync()
    {
        this.connection?.Dispose();
        this.connection = null;
        return ValueTask.CompletedTask;
    }
}
