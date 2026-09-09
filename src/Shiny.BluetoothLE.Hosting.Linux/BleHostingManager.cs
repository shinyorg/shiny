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
    DBusConnection? connection;
    LEAdvertisement? advertisement;
    Task? pendingUnregister;
    int advertisementCounter;


    public BleHostingManager(ILogger<BleHostingManager> logger)
    {
        this.logger = logger;
    }


    public AccessState AdvertisingAccessStatus { get; private set; } = AccessState.Unknown;
    public AccessState GattAccessStatus { get; private set; } = AccessState.Unknown;
    public bool IsAdvertising => this.advertisement != null;
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

        // TODO: export GattService1/GattCharacteristic1 D-Bus objects via DBusConnection.AddMethodHandler
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


    public Task StartAdvertising(AdvertisementOptions? options = null)
    {
        options ??= new AdvertisementOptions();

        var serviceData = options.ServiceData.ToDictionary(
            x => NormalizeUuid(x.Uuid),
            x => x.Data
        );

        var manufacturerData = new Dictionary<ushort, byte[]>();
        if (options.ManufacturerData != null)
            manufacturerData[options.ManufacturerData.CompanyId] = options.ManufacturerData.Data;

        return this.RegisterAdvertisement(new LEAdvertisementProperties
        {
            Type = options.IsConnectable ? "peripheral" : "broadcast",
            LocalName = options.LocalName.IsEmpty() ? null : options.LocalName,
            ServiceUuids = options.ServiceUuids.Select(NormalizeUuid).ToList(),
            ServiceData = serviceData,
            ManufacturerData = manufacturerData,
            Includes = options.IncludeTxPower ? ["tx-power"] : []
        });
    }


    public Task AdvertiseBeacon(Guid uuid, ushort major, ushort minor, sbyte? txpower = null)
        => this.RegisterAdvertisement(new LEAdvertisementProperties
        {
            // A beacon has no GATT server to connect to, and a connectable advertisement would
            // invite centrals to try. Broadcast also means Discoverable is not reported at all -
            // BlueZ rejects the advertisement outright if both are set.
            Type = "broadcast",
            ManufacturerData = new Dictionary<ushort, byte[]>
            {
                [IBeaconPacket.AppleCompanyId] = IBeaconPacket.Build(
                    uuid,
                    major,
                    minor,
                    txpower ?? IBeaconPacket.DefaultTxPower
                )
            }
        });


    public void StopAdvertising()
    {
        var current = this.advertisement;
        if (current == null)
            return;

        this.advertisement = null;

        // Unregistering is a round trip to BlueZ, but the interface is synchronous, so the call is
        // kept as a task the next StartAdvertising awaits rather than being blocked on here. The
        // exported object goes away immediately, which is what stops BlueZ reading from us.
        this.connection?.RemoveMethodHandler(current.Path);
        this.pendingUnregister = this.UnregisterAdvertisement(current.Path);
    }


    async Task RegisterAdvertisement(LEAdvertisementProperties properties)
    {
        await this.EnsureConnectionAsync().ConfigureAwait(false);

        if (this.advertisement != null)
            throw new InvalidOperationException("An advertisement is already running - call StopAdvertising first");

        // let a Stop that is still in flight finish, so BlueZ is not holding the old registration
        // when the new one arrives
        var pending = this.pendingUnregister;
        if (pending != null)
        {
            await pending.ConfigureAwait(false);
            this.pendingUnregister = null;
        }

        // A fresh path per registration. BlueZ answers "Already Exists" if a path it still knows
        // about is re-registered, and reusing one makes a Stop/Start race unnecessarily fragile.
        var path = BluezConstants.AdvertisementPathPrefix + Interlocked.Increment(ref this.advertisementCounter);

        var export = new LEAdvertisement(
            path,
            properties,
            () => this.OnAdvertisementReleased(path)
        );

        this.connection!.AddMethodHandler(export);

        try
        {
            var writer = this.connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: BluezConstants.Service,
                path: BluezConstants.DefaultAdapterPath,
                @interface: BluezConstants.LEAdvertisingManagerInterface,
                member: "RegisterAdvertisement",
                signature: "oa{sv}"
            );
            writer.WriteObjectPath(path);

            // no registration options - BlueZ defines none that matter here
            var dict = writer.WriteDictionaryStart();
            writer.WriteDictionaryEnd(dict);

            await this.connection.CallMethodAsync(writer.CreateMessage()).ConfigureAwait(false);
        }
        catch
        {
            // BlueZ read our properties and refused them (or never answered) - do not leave an
            // object exported that nothing is going to call
            this.connection.RemoveMethodHandler(path);
            throw;
        }

        this.advertisement = export;
        this.logger.LogInformation("Registered BlueZ advertisement at {Path}", path);
    }


    async Task UnregisterAdvertisement(string path)
    {
        try
        {
            var writer = this.connection!.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: BluezConstants.Service,
                path: BluezConstants.DefaultAdapterPath,
                @interface: BluezConstants.LEAdvertisingManagerInterface,
                member: "UnregisterAdvertisement",
                signature: "o"
            );
            writer.WriteObjectPath(path);

            await this.connection.CallMethodAsync(writer.CreateMessage()).ConfigureAwait(false);
            this.logger.LogInformation("Unregistered BlueZ advertisement at {Path}", path);
        }
        catch (Exception ex)
        {
            // DoesNotExist here means BlueZ already dropped it - a Release we raced, or the daemon
            // restarting. Either way the advertisement is gone, which is what the caller wanted.
            this.logger.LogDebug(ex, "Could not unregister BlueZ advertisement at {Path}", path);
        }
    }


    void OnAdvertisementReleased(string path)
    {
        // BlueZ stopped the advertisement on its own - adapter powered down, another client took
        // the slot, or bluetoothd restarted. Drop our side so IsAdvertising stops lying and a
        // later StartAdvertising is not refused as "already running".
        if (this.advertisement?.Path != path)
            return;

        this.logger.LogInformation("BlueZ released the advertisement at {Path}", path);
        this.advertisement = null;
        this.connection?.RemoveMethodHandler(path);
    }


    /// <summary>
    /// BlueZ wants 128-bit UUIDs in the long lowercase form; callers routinely pass the 16-bit
    /// short form that every other platform accepts.
    /// </summary>
    static string NormalizeUuid(string uuid)
        => (uuid.Length == 4 ? $"0000{uuid}-0000-1000-8000-00805F9B34FB" : uuid).ToLowerInvariant();


    public Task<L2CapInstance> OpenL2Cap(bool secure, Action<L2CapChannel> onOpen)
    {
        // L2CAP CoC is independent of BlueZ's GATT surface — it goes straight to the kernel via
        // AF_BLUETOOTH sockets, so this works even though the GATT server is still a stub.
        var (listener, psm) = L2CapSocket.Listen(secure);
        var cts = new CancellationTokenSource();

        _ = Task.Run(() =>
        {
            while (!cts.IsCancellationRequested)
            {
                L2CapSocket.L2CapHandle? client = null;
                string? peer = null;
                try
                {
                    var accepted = L2CapSocket.Accept(listener);
                    if (accepted == null || cts.IsCancellationRequested)
                    {
                        accepted?.Client.Dispose();
                        return;
                    }
                    (client, peer) = (accepted.Value.Client, accepted.Value.PeerAddress);
                }
                catch (Exception ex) when (!cts.IsCancellationRequested)
                {
                    this.logger.LogWarning(ex, "L2CAP accept failed on PSM {Psm}", psm);
                    continue;
                }

                var connectionCts = new CancellationTokenSource();
                var rxSubj = new System.Reactive.Subjects.Subject<byte[]>();
                var connected = client!;

                _ = Task.Run(() =>
                {
                    try
                    {
                        while (!connectionCts.IsCancellationRequested)
                        {
                            var frame = connected.Receive();
                            if (frame == null) break;
                            rxSubj.OnNext(frame);
                        }
                        rxSubj.OnCompleted();
                    }
                    catch (Exception ex)
                    {
                        rxSubj.OnError(ex);
                    }
                });

                onOpen(new L2CapChannel(
                    psm,
                    peer!,
                    data => System.Reactive.Linq.Observable.FromAsync(ct => connected.SendAsync(data, ct)),
                    rxSubj,
                    () =>
                    {
                        connectionCts.Cancel();
                        connected.Dispose();
                    }
                ));
            }
        });

        return Task.FromResult(new L2CapInstance(
            psm,
            () =>
            {
                cts.Cancel();
                listener.Dispose();
            }
        ));
    }


    async Task EnsureConnectionAsync(CancellationToken ct = default)
    {
        if (this.connection != null) return;
        this.connection = new DBusConnection(DBusAddress.System!);
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


    public async ValueTask DisposeAsync()
    {
        // BlueZ keeps advertising until the registration is dropped, and dropping the connection
        // underneath it leaves the daemon holding a registration for a client that no longer
        // answers - so unregister first and wait for it.
        this.StopAdvertising();

        var pending = this.pendingUnregister;
        if (pending != null)
        {
            try
            {
                await pending.ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                this.logger.LogDebug(ex, "Error unregistering the advertisement during dispose");
            }
            this.pendingUnregister = null;
        }

        this.connection?.Dispose();
        this.connection = null;
    }
}
