using System;
using System.Collections.Concurrent;
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
public class BleHostingManager : IBleHostingManager, IBluezGattHost, IAsyncDisposable
{
    readonly ILogger<BleHostingManager> logger;
    readonly Dictionary<string, GattService> services = new();
    readonly SemaphoreSlim gattLock = new(1, 1);
    readonly ConcurrentDictionary<string, Peripheral> peripherals = new();
    readonly HashSet<string> connectedDevices = new();
    DBusConnection? connection;
    LEAdvertisement? advertisement;
    Task? pendingUnregister;
    Task? pendingGattUpdate;
    IDisposable? deviceWatch;
    bool applicationExported;
    bool applicationRegistered;
    int advertisementCounter;
    int serviceCounter;


    public BleHostingManager(ILogger<BleHostingManager> logger)
    {
        this.logger = logger;
    }


    public AccessState AdvertisingAccessStatus { get; private set; } = AccessState.Unknown;
    public AccessState GattAccessStatus { get; private set; } = AccessState.Unknown;
    public bool IsAdvertising => this.advertisement != null;
    public IReadOnlyList<IGattService> Services => this.GetServiceSnapshot().Cast<IGattService>().ToList();


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

        var service = new GattService(uuid, primary);
        serviceBuilder(service);

        // a fresh path per service - numbering from the service count collides once one has been removed
        service.ObjectPath = $"{BluezConstants.ApplicationRootPath}/service{Interlocked.Increment(ref this.serviceCounter)}";
        for (var i = 0; i < service.NativeCharacteristics.Count; i++)
        {
            var characteristic = service.NativeCharacteristics[i];
            characteristic.ObjectPath = $"{service.ObjectPath}/char{i}";
            characteristic.ServicePath = service.ObjectPath;
        }

        await this.gattLock.WaitAsync().ConfigureAwait(false);
        try
        {
            lock (this.services)
            {
                if (this.services.ContainsKey(uuid))
                    throw new InvalidOperationException($"Service '{uuid}' is already registered");
            }

            await this.EnsureDeviceWatchAsync().ConfigureAwait(false);

            // BlueZ reads the object tree once, during registration: objects added afterwards are ignored and
            // removing one drops the whole application. Any change is unregister, change the tree, register again
            await this.UnregisterApplication().ConfigureAwait(false);

            if (!this.applicationExported)
            {
                this.connection!.AddMethodHandler(new GattApplicationObject(BluezConstants.ApplicationRootPath, this.GetServiceSnapshot));
                this.applicationExported = true;
            }

            var handlers = this.CreateHandlers(service);
            this.connection!.AddMethodHandlers(handlers);
            foreach (var characteristic in service.NativeCharacteristics)
                characteristic.NotifyDispatcher = data => this.EmitValueChanged(characteristic, data);

            lock (this.services)
                this.services.Add(uuid, service);

            try
            {
                await this.RegisterApplication().ConfigureAwait(false);
            }
            catch
            {
                // BlueZ refused the tree - take this service back out and restore the ones that were running
                lock (this.services)
                    this.services.Remove(uuid);

                Detach(service);
                this.connection.RemoveMethodHandlers(HandlerPaths(service));
                await this.TryRegisterApplication().ConfigureAwait(false);
                throw;
            }
        }
        finally
        {
            this.gattLock.Release();
        }

        this.logger.LogInformation("Registered GATT service {Uuid} at {Path}", uuid, service.ObjectPath);
        return service;
    }


    public void RemoveService(string serviceUuid)
    {
        GattService? service;
        lock (this.services)
        {
            if (!this.services.Remove(serviceUuid, out service))
                return;
        }

        Detach(service);
        this.QueueApplicationRebuild(HandlerPaths(service));
    }


    public void ClearServices()
    {
        List<GattService> removed;
        lock (this.services)
        {
            removed = this.services.Values.ToList();
            this.services.Clear();
        }

        if (removed.Count == 0)
            return;

        foreach (var service in removed)
            Detach(service);

        this.QueueApplicationRebuild(removed.SelectMany(HandlerPaths).ToList());
    }


    public Task StartAdvertising(AdvertisementOptions? options = null)
    {
        options ??= new AdvertisementOptions();

        var serviceData = options.ServiceData.ToDictionary(
            x => BluezGatt.NormalizeUuid(x.Uuid),
            x => x.Data
        );

        var manufacturerData = new Dictionary<ushort, byte[]>();
        if (options.ManufacturerData != null)
            manufacturerData[options.ManufacturerData.CompanyId] = options.ManufacturerData.Data;

        return this.RegisterAdvertisement(new LEAdvertisementProperties
        {
            Type = options.IsConnectable ? "peripheral" : "broadcast",
            LocalName = options.LocalName.IsEmpty() ? null : options.LocalName,
            ServiceUuids = options.ServiceUuids.Select(BluezGatt.NormalizeUuid).ToList(),
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


    public Task<L2CapInstance> OpenL2Cap(bool secure, Action<L2CapChannel> onOpen)
    {
        // L2CAP CoC is independent of BlueZ's GATT surface — it goes straight to the kernel via
        // AF_BLUETOOTH sockets rather than through D-Bus.
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


    // ---- GATT application --------------------------------------------------------------------

    ILogger IBluezGattHost.Logger => this.logger;
    Peripheral IBluezGattHost.GetPeripheral(string? devicePath, ushort mtu) => this.GetPeripheral(devicePath, mtu);


    void IBluezGattHost.SetNotifying(GattCharacteristic characteristic, bool notifying)
    {
        List<Peripheral> connected;
        lock (this.connectedDevices)
            connected = this.connectedDevices.Select(x => this.GetPeripheral(x)).ToList();

        characteristic.SetNotifying(notifying, connected);
    }


    Peripheral GetPeripheral(string? devicePath, ushort mtu = 0)
    {
        // BlueZ versions that do not pass the device option share one anonymous central
        var peripheral = this.peripherals.GetOrAdd(
            devicePath ?? String.Empty,
            static path => new Peripheral(path, BluezGatt.AddressFromDevicePath(path) ?? path)
        );

        // BlueZ reports the ATT MTU; IPeripheral.Mtu is the usable payload on every platform
        if (mtu > BleConstants.AttHeaderSize)
            peripheral.Mtu = mtu - BleConstants.AttHeaderSize;

        return peripheral;
    }


    IReadOnlyList<GattService> GetServiceSnapshot()
    {
        lock (this.services)
            return this.services.Values.ToList();
    }


    List<IPathMethodHandler> CreateHandlers(GattService service)
    {
        var handlers = new List<IPathMethodHandler> { new GattServiceObject(service) };
        handlers.AddRange(service.NativeCharacteristics.Select(x => new GattCharacteristicObject(x, this)));
        return handlers;
    }


    static List<string> HandlerPaths(GattService service) => service
        .NativeCharacteristics
        .Select(x => x.ObjectPath!)
        .Prepend(service.ObjectPath!)
        .ToList();


    static void Detach(GattService service)
    {
        foreach (var characteristic in service.NativeCharacteristics)
        {
            characteristic.NotifyDispatcher = null;
            characteristic.SetNotifying(false, []);
        }
    }


    void QueueApplicationRebuild(List<string> removedPaths)
    {
        // Unregistering is a round trip to BlueZ but RemoveService/ClearServices are synchronous, so the
        // rebuild runs as a task instead of being blocked on - the next AddService queues behind it on the lock
        var previous = this.pendingGattUpdate ?? Task.CompletedTask;
        this.pendingGattUpdate = Task.WhenAll(previous, this.RebuildApplication(removedPaths));
    }


    async Task RebuildApplication(List<string> removedPaths)
    {
        await this.gattLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await this.UnregisterApplication().ConfigureAwait(false);
            this.connection?.RemoveMethodHandlers(removedPaths);
            await this.TryRegisterApplication().ConfigureAwait(false);
        }
        finally
        {
            this.gattLock.Release();
        }
    }


    async Task TryRegisterApplication()
    {
        if (this.GetServiceSnapshot().Count == 0)
            return;

        try
        {
            await this.RegisterApplication().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Could not register the remaining GATT services with BlueZ");
        }
    }


    async Task RegisterApplication()
    {
        var writer = this.connection!.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: BluezConstants.Service,
            path: BluezConstants.DefaultAdapterPath,
            @interface: BluezConstants.GattManagerInterface,
            member: "RegisterApplication",
            signature: "oa{sv}"
        );
        writer.WriteObjectPath(BluezConstants.ApplicationRootPath);

        // no registration options - BlueZ defines none for a GATT server
        var dict = writer.WriteDictionaryStart();
        writer.WriteDictionaryEnd(dict);

        // BlueZ calls GetManagedObjects on the application root while this is in flight
        await this.connection.CallMethodAsync(writer.CreateMessage()).ConfigureAwait(false);
        this.applicationRegistered = true;
    }


    async Task UnregisterApplication()
    {
        if (!this.applicationRegistered || this.connection == null)
            return;

        this.applicationRegistered = false;
        try
        {
            var writer = this.connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: BluezConstants.Service,
                path: BluezConstants.DefaultAdapterPath,
                @interface: BluezConstants.GattManagerInterface,
                member: "UnregisterApplication",
                signature: "o"
            );
            writer.WriteObjectPath(BluezConstants.ApplicationRootPath);
            await this.connection.CallMethodAsync(writer.CreateMessage()).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // DoesNotExist means BlueZ already dropped it - bluetoothd restarted, say. It is gone either way
            this.logger.LogDebug(ex, "Could not unregister the GATT application from BlueZ");
        }
    }


    void EmitValueChanged(GattCharacteristic characteristic, byte[] value)
    {
        var connection = this.connection ?? throw new InvalidOperationException("Not connected to the system D-Bus");

        // BlueZ turns a PropertiesChanged on Value into a notification or indication to every subscribed central
        var writer = connection.GetMessageWriter();
        writer.WriteSignalHeader(
            path: characteristic.ObjectPath,
            @interface: BluezConstants.PropertiesInterface,
            member: "PropertiesChanged",
            signature: "sa{sv}as"
        );
        writer.WriteString(BluezConstants.GattCharacteristicInterface);

        var changed = writer.WriteDictionaryStart();
        writer.WriteDictionaryEntryStart();
        writer.WriteString("Value");
        writer.WriteSignature("ay");
        writer.WriteArray(value);
        writer.WriteDictionaryEnd(changed);

        writer.WriteArray(Array.Empty<string>());

        if (!connection.TrySendMessage(writer.CreateMessage()))
            throw new InvalidOperationException("The D-Bus connection is closed - the notification was not sent");
    }


    // ---- connected centrals ------------------------------------------------------------------

    async Task EnsureDeviceWatchAsync()
    {
        if (this.deviceWatch != null)
            return;

        // watch first, then read the current state, so a connection that lands in between is not missed
        this.deviceWatch = await this.connection!.WatchSignalAsync(
            BluezConstants.Service,
            null,
            BluezConstants.PropertiesInterface,
            "PropertiesChanged",
            static (Message message, object? _) => ReadConnectionChange(message),
            (Exception? ex, (string DevicePath, bool Connected)? change) =>
            {
                if (ex == null && change != null)
                    this.OnDeviceConnectionChanged(change.Value.DevicePath, change.Value.Connected);
            },
            null,
            false,
            ObserverFlags.None
        ).ConfigureAwait(false);

        var writer = this.connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: BluezConstants.Service,
            path: "/",
            @interface: BluezConstants.ObjectManagerInterface,
            member: "GetManagedObjects"
        );
        var connected = await this.connection
            .CallMethodAsync(writer.CreateMessage(), static (Message reply, object? _) => ReadConnectedDevices(reply))
            .ConfigureAwait(false);

        foreach (var devicePath in connected)
            this.OnDeviceConnectionChanged(devicePath, true);
    }


    static (string DevicePath, bool Connected)? ReadConnectionChange(Message message)
    {
        var path = message.PathAsString;
        if (path == null || !path.StartsWith(BluezConstants.DefaultAdapterPath + "/dev_", StringComparison.Ordinal))
            return null;

        var reader = message.GetBodyReader();
        if (reader.ReadString() != BluezConstants.DeviceInterface)
            return null;

        var changed = reader.ReadDictionaryOfStringToVariantValue();
        return changed.TryGetValue("Connected", out var connected)
            ? (path, connected.GetBool())
            : null;
    }


    static List<string> ReadConnectedDevices(Message reply)
    {
        var connected = new List<string>();
        var reader = reply.GetBodyReader();

        var objects = reader.ReadDictionaryStart();
        while (reader.HasNext(objects))
        {
            reader.AlignStruct();
            var path = reader.ReadObjectPathAsString();

            var interfaces = reader.ReadDictionaryStart();
            while (reader.HasNext(interfaces))
            {
                reader.AlignStruct();
                var interfaceName = reader.ReadString();
                var properties = reader.ReadDictionaryOfStringToVariantValue();

                var isConnectedDevice =
                    interfaceName == BluezConstants.DeviceInterface &&
                    path.StartsWith(BluezConstants.DefaultAdapterPath + "/", StringComparison.Ordinal) &&
                    properties.TryGetValue("Connected", out var value) &&
                    value.GetBool();

                if (isConnectedDevice)
                    connected.Add(path);
            }
        }
        return connected;
    }


    void OnDeviceConnectionChanged(string devicePath, bool connected)
    {
        bool changed;
        lock (this.connectedDevices)
            changed = connected ? this.connectedDevices.Add(devicePath) : this.connectedDevices.Remove(devicePath);

        if (!changed)
            return;

        var peripheral = this.GetPeripheral(devicePath);
        foreach (var service in this.GetServiceSnapshot())
        {
            foreach (var characteristic in service.NativeCharacteristics)
                characteristic.OnDeviceConnectionChanged(peripheral, connected);
        }
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

        // the same goes for the GATT application
        var gattUpdate = this.pendingGattUpdate;
        if (gattUpdate != null)
            await gattUpdate.ConfigureAwait(false);

        await this.gattLock.WaitAsync().ConfigureAwait(false);
        try
        {
            await this.UnregisterApplication().ConfigureAwait(false);
        }
        finally
        {
            this.gattLock.Release();
        }

        this.deviceWatch?.Dispose();
        this.deviceWatch = null;

        this.connection?.Dispose();
        this.connection = null;
    }
}
