using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.BluetoothLE.Bluez;
using Shiny.BluetoothLE.Intrastructure;
using Tmds.DBus.Protocol;
using MessageNotification = Tmds.DBus.Protocol.Notification<Tmds.DBus.Protocol.Message>;
using Shiny.BluetoothLE.Infrastructure;

namespace Shiny.BluetoothLE;


public class BleManager : INativeBleManager, IAsyncDisposable
{
    readonly IOperationQueue operations;
    readonly ILogger<BleManager> logger;
    readonly ILogger<Peripheral> peripheralLogger;
    DBusConnection? connection;
    bool ownsConnection = true;
    string adapterPath = BluezConstants.DefaultAdapterPath;
    Task? connecting;
    BluezAdapter? adapter;
    readonly ConcurrentDictionary<string, Peripheral> peripherals = new();


    public BleManager(
        IOperationQueue operations,
        ILogger<BleManager> logger,
        ILogger<Peripheral> peripheralLogger)
    {
        this.operations = operations;
        this.logger = logger;
        this.peripheralLogger = peripheralLogger;
    }


    public bool IsScanning { get; private set; }
    public AccessState CurrentAccess => AccessState.Unknown;

    /// <summary>
    /// The system bus connection, or null until Bluetooth is first used.
    /// </summary>
    public DBusConnection? Connection => this.connection;

    /// <summary>
    /// The BlueZ adapter object path (e.g. <c>/org/bluez/hci0</c>), or null until Bluetooth is first used.
    /// </summary>
    public string? AdapterObjectPath => this.adapter?.ObjectPath;

    object? INativeBleManager.NativeObject => this.AdapterObjectPath;
    public event EventHandler<object>? NativeObjectLoaded;

    /// <summary>
    /// Uses a system bus connection and/or BlueZ adapter supplied by the caller instead of the system bus and
    /// <c>/org/bluez/hci0</c>. Only before Bluetooth is first used - it throws once the connection exists. A supplied
    /// connection must already be connected, and is not disposed with this manager.
    /// <b>Advanced - only with deep knowledge of BlueZ and D-Bus.</b> Shiny assumes it owns the connection, and one
    /// shared with other code can silently break discovery and notifications (a handler that throws on it
    /// disconnects everything).
    /// </summary>
    /// <param name="connection">A connected system bus connection, or null to let Shiny open its own.</param>
    /// <param name="adapterObjectPath">The BlueZ adapter object path (e.g. <c>/org/bluez/hci1</c>), or null for <c>/org/bluez/hci0</c>.</param>
    public void SetConnection(DBusConnection? connection, string? adapterObjectPath = null)
    {
        if (this.connection != null)
            throw new InvalidOperationException("The D-Bus connection already exists - it can only be set before Bluetooth is first used");

        if (adapterObjectPath != null)
        {
            if (!adapterObjectPath.StartsWith("/org/bluez/"))
                throw new ArgumentException("Expected a BlueZ adapter object path such as /org/bluez/hci1", nameof(adapterObjectPath));

            this.adapterPath = adapterObjectPath.TrimEnd('/');
        }
        if (connection != null)
        {
            this.connection = connection;
            this.connecting = Task.CompletedTask;
            this.ownsConnection = false;
        }
        this.logger.LogWarning("The BlueZ connection/adapter was set externally ({AdapterPath})", this.adapterPath);
    }


    Task INativeBleManager.SetNativeObjectAsync(object native, CancellationToken cancelToken)
    {
        switch (native)
        {
            case string path:
                this.SetConnection(null, path);
                break;

            case DBusConnection conn:
                this.SetConnection(conn);
                break;

            default:
                throw new ArgumentException($"Expected a BlueZ adapter object path or a DBusConnection, got {native?.GetType().FullName ?? "null"}", nameof(native));
        }
        return Task.CompletedTask;
    }


    void RaiseNativeObjectLoaded(object native)
    {
        // a diagnostics subscriber must never break the call that loaded the native object
        try
        {
            this.NativeObjectLoaded?.Invoke(this, native);
        }
        catch (Exception ex)
        {
            this.logger.LogWarning(ex, "A NativeObjectLoaded handler threw");
        }
    }


    async Task<DBusConnection> GetConnectionAsync(CancellationToken ct = default)
    {
        if (this.connection == null)
        {
            this.connection = new DBusConnection(DBusAddress.System!);
            this.connecting = this.connection.ConnectAsync().AsTask();
        }
        // every caller waits on the one connect, so a caller that cancels stops waiting without
        // abandoning the connection for the others - and none of them gets it before it is up
        await this.connecting!.WaitAsync(ct).ConfigureAwait(false);
        return this.connection;
    }


    async Task<BluezAdapter> GetAdapterAsync(CancellationToken ct = default)
    {
        if (this.adapter == null)
        {
            var conn = await this.GetConnectionAsync(ct).ConfigureAwait(false);
            // two callers can both get here - only the one that wins reports the load
            var created = new BluezAdapter(conn, this.adapterPath);
            if (Interlocked.CompareExchange(ref this.adapter, created, null) == null)
                this.RaiseNativeObjectLoaded(created.ObjectPath);
        }
        return this.adapter;
    }


    public IObservable<AccessState> RequestAccess() => ObservableTask.FromAsync(async ct =>
    {
        try
        {
            var adapter = await this.GetAdapterAsync(ct).ConfigureAwait(false);
            var powered = await adapter.GetPoweredAsync(ct).ConfigureAwait(false);
            return powered ? AccessState.Available : AccessState.Disabled;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return AccessState.NotSupported;
        }
    });


    public IPeripheral? GetKnownPeripheral(string peripheralUuid)
        => this.peripherals.Values.FirstOrDefault(x => x.Uuid.Equals(peripheralUuid, StringComparison.InvariantCultureIgnoreCase));


    public IEnumerable<IPeripheral> GetConnectedPeripherals()
        => this.peripherals.Values.Where(x => x.Status == ConnectionState.Connected);


    public void StopScan()
    {
        this.IsScanning = false;
    }


    public IObservable<ScanResult> Scan(ScanConfig? scanConfig = null) => Observable.Create<ScanResult>(ob =>
    {
        if (this.IsScanning)
            throw new InvalidOperationException("There is already an existing scan");

        this.IsScanning = true;
        var cts = new CancellationTokenSource();
        var disposables = new List<IDisposable>();

        _ = Task.Run(async () =>
        {
            try
            {
                var conn = await this.GetConnectionAsync(cts.Token).ConfigureAwait(false);
                var adapter = await this.GetAdapterAsync(cts.Token).ConfigureAwait(false);

                // Set LE discovery filter
                try
                {
                    await adapter.SetDiscoveryFilterAsync("le", cts.Token).ConfigureAwait(false);
                }
                catch (Exception ex) when (ex is not OperationCanceledException)
                {
                    // filter may not be supported - proceed anyway
                }

                // Watch for InterfacesAdded signals on ObjectManager for new devices
                var ifaceRule = new MatchRule
                {
                    Type = MessageType.Signal,
                    Interface = BluezConstants.ObjectManagerInterface,
                    Member = "InterfacesAdded"
                };

                var ifaceSub = await conn.AddMatchAsync(
                    ifaceRule,
                    static (Message msg, object? _) => msg,
                    static (MessageNotification n) =>
                    {
                        // IsCompletion first: on a value notification, reading Exception throws, and a
                        // handler that throws makes Tmds disconnect the whole connection - which ends
                        // BlueZ discovery, notifications and every other call on it.
                        if (n.IsCompletion) return;
                        var ctx = (ScanContext)n.State!;
                        try
                        {
                            ctx.Manager.ProcessInterfacesAdded(n.Value, ctx.Observer);
                        }
                        catch (Exception exc)
                        {
                            ctx.Manager.logger.BluezError(exc, "Error processing InterfacesAdded");
                        }
                    },
                    emitOnCapturedContext: false,
                    ObserverFlags.None,
                    new ScanContext(this, ob)
                ).ConfigureAwait(false);
                disposables.Add(ifaceSub);

                // Also watch PropertiesChanged on device paths for RSSI updates
                var propsRule = new MatchRule
                {
                    Type = MessageType.Signal,
                    Interface = BluezConstants.PropertiesInterface,
                    Member = "PropertiesChanged"
                };

                var propsSub = await conn.AddMatchAsync(
                    propsRule,
                    static (Message msg, object? _) => msg,
                    static (MessageNotification n) =>
                    {
                        // IsCompletion first: on a value notification, reading Exception throws, and a
                        // handler that throws makes Tmds disconnect the whole connection - which ends
                        // BlueZ discovery, notifications and every other call on it.
                        if (n.IsCompletion) return;
                        var ctx = (ScanContext)n.State!;
                        try
                        {
                            ctx.Manager.ProcessPropertiesChanged(n.Value, ctx.Observer);
                        }
                        catch (Exception exc)
                        {
                            ctx.Manager.logger.BluezError(exc, "Error processing PropertiesChanged");
                        }
                    },
                    emitOnCapturedContext: false,
                    ObserverFlags.None,
                    new ScanContext(this, ob)
                ).ConfigureAwait(false);
                disposables.Add(propsSub);

                // Emit scan results for already-known devices
                await this.EmitExistingDevicesAsync(conn, ob, scanConfig, cts.Token).ConfigureAwait(false);

                // Start discovery
                await adapter.StartDiscoveryAsync(cts.Token).ConfigureAwait(false);
                this.logger.ScanStarted();
            }
            catch (Exception ex)
            {
                if (!cts.IsCancellationRequested)
                    ob.OnError(ex);
            }
        }, cts.Token);

        return () =>
        {
            this.IsScanning = false;
            cts.Cancel();
            foreach (var d in disposables) d.Dispose();
            cts.Dispose();
            this.logger.ScanStopped();

            _ = Task.Run(async () =>
            {
                try
                {
                    var adapter = await this.GetAdapterAsync().ConfigureAwait(false);
                    await adapter.StopDiscoveryAsync().ConfigureAwait(false);
                }
                catch
                {
                    // best effort
                }
            });
        };
    });


    record ScanContext(BleManager Manager, IObserver<ScanResult> Observer);


    void ProcessInterfacesAdded(Message message, IObserver<ScanResult> ob)
    {
        var reader = message.GetBodyReader();
        var objectPath = (string)reader.ReadObjectPath();

        // Check if this is a device under our adapter
        if (!objectPath.StartsWith(this.adapterPath + "/"))
            return;

        var interfacesEnd = reader.ReadArrayStart(DBusType.DictEntry);
        while (reader.HasNext(interfacesEnd))
        {
            var iface = reader.ReadString();
            var propsEnd = reader.ReadArrayStart(DBusType.DictEntry);

            if (iface == BluezConstants.DeviceInterface)
            {
                string? name = null;
                string? address = null;
                short rssi = 0;
                string[]? uuids = null;
                short txPower = 0;

                while (reader.HasNext(propsEnd))
                {
                    var propName = reader.ReadString();
                    switch (propName)
                    {
                        case "Name":
                            name = reader.ReadStringVariant();
                            break;
                        case "Address":
                            address = reader.ReadStringVariant();
                            break;
                        case "RSSI":
                            rssi = reader.ReadInt16Variant();
                            break;
                        case "UUIDs":
                            uuids = reader.ReadStringArrayVariant();
                            break;
                        case "TxPower":
                            txPower = reader.ReadInt16Variant();
                            break;
                        default:
                            reader.SkipVariant();
                            break;
                    }
                }

                if (address != null)
                {
                    var peripheral = this.GetOrCreatePeripheral(objectPath, address, name);
                    var ad = AdvertisementData.FromDeviceProperties(name, uuids, txPower, null, null);
                    ob.OnNext(new ScanResult(peripheral, rssi, ad));
                }
            }
            else
            {
                while (reader.HasNext(propsEnd))
                {
                    reader.ReadString();
                    reader.SkipVariant();
                }
            }
        }
    }


    void ProcessPropertiesChanged(Message message, IObserver<ScanResult> ob)
    {
        var path = message.PathAsString;
        if (path == null || !path.StartsWith(this.adapterPath + "/dev_"))
            return;

        var reader = message.GetBodyReader();
        var iface = reader.ReadString();
        if (iface != BluezConstants.DeviceInterface)
            return;

        // Check if we already know this peripheral
        if (!this.peripherals.TryGetValue(path, out var peripheral))
            return;

        short rssi = 0;
        string? name = null;
        bool hasUpdate = false;

        var propsEnd = reader.ReadArrayStart(DBusType.DictEntry);
        while (reader.HasNext(propsEnd))
        {
            var propName = reader.ReadString();
            switch (propName)
            {
                case "RSSI":
                    rssi = reader.ReadInt16Variant();
                    hasUpdate = true;
                    break;
                case "Name":
                    name = reader.ReadStringVariant();
                    peripheral.Name = name;
                    hasUpdate = true;
                    break;
                case "Connected":
                    var connected = reader.ReadBoolVariant();
                    peripheral.ReceiveConnectionChange(connected);
                    break;
                default:
                    reader.SkipVariant();
                    break;
            }
        }

        if (hasUpdate && this.IsScanning)
        {
            var ad = AdvertisementData.FromDeviceProperties(peripheral.Name, null, 0, null, null);
            ob.OnNext(new ScanResult(peripheral, rssi, ad));
        }
    }


    async Task EmitExistingDevicesAsync(DBusConnection conn, IObserver<ScanResult> ob, ScanConfig? config, CancellationToken ct)
    {
        var msg = conn.CreateMethodCall(
            BluezConstants.Service,
            "/",
            BluezConstants.ObjectManagerInterface,
            "GetManagedObjects"
        );

        // GetManagedObjects returns a{oa{sa{sv}}} which we parse via MessageValueReader
        var devices = await conn.CallAsync(
            msg,
            static (Message reply, object? state) =>
            {
                var result = new List<(string path, string? name, string? address, short rssi, string[]? uuids, short txPower)>();
                var devicePrefix = (string)state!;
                var reader = reply.GetBodyReader();

                var objectsEnd = reader.ReadArrayStart(DBusType.DictEntry);
                while (reader.HasNext(objectsEnd))
                {
                    var path = (string)reader.ReadObjectPath();
                    var interfacesEnd = reader.ReadArrayStart(DBusType.DictEntry);

                    string? name = null;
                    string? address = null;
                    short rssi = 0;
                    string[]? uuids = null;
                    short txPower = 0;
                    bool isDevice = false;

                    while (reader.HasNext(interfacesEnd))
                    {
                        var iface = reader.ReadString();
                        var propsEnd = reader.ReadArrayStart(DBusType.DictEntry);

                        if (iface == BluezConstants.DeviceInterface && path.StartsWith(devicePrefix))
                        {
                            isDevice = true;
                            while (reader.HasNext(propsEnd))
                            {
                                var propName = reader.ReadString();
                                switch (propName)
                                {
                                    case "Name":
                                        name = reader.ReadStringVariant();
                                        break;
                                    case "Address":
                                        address = reader.ReadStringVariant();
                                        break;
                                    case "RSSI":
                                        rssi = reader.ReadInt16Variant();
                                        break;
                                    case "UUIDs":
                                        uuids = reader.ReadStringArrayVariant();
                                        break;
                                    case "TxPower":
                                        txPower = reader.ReadInt16Variant();
                                        break;
                                    default:
                                        reader.SkipVariant();
                                        break;
                                }
                            }
                        }
                        else
                        {
                            while (reader.HasNext(propsEnd))
                            {
                                reader.ReadString();
                                reader.SkipVariant();
                            }
                        }
                    }

                    if (isDevice && address != null)
                        result.Add((path, name, address, rssi, uuids, txPower));
                }

                return result;
            },
            ct,
            this.adapterPath + "/"
        ).ConfigureAwait(false);

        foreach (var dev in devices)
        {
            // Filter by service UUIDs if configured
            if (config?.ServiceUuids != null && config.ServiceUuids.Length > 0)
            {
                if (dev.uuids == null || !config.ServiceUuids.Any(su => dev.uuids.Any(u => u.Equals(su, StringComparison.OrdinalIgnoreCase))))
                    continue;
            }

            var peripheral = this.GetOrCreatePeripheral(dev.path, dev.address!, dev.name);
            var ad = AdvertisementData.FromDeviceProperties(dev.name, dev.uuids, dev.txPower, null, null);
            ob.OnNext(new ScanResult(peripheral, dev.rssi, ad));
        }
    }


    Peripheral GetOrCreatePeripheral(string objectPath, string uuid, string? name)
    {
        return this.peripherals.GetOrAdd(objectPath, _ =>
        {
            this.logger.DeviceDiscovered(objectPath);
            return new Peripheral(
                this.connection!,
                objectPath,
                uuid,
                name,
                this.operations,
                this.peripheralLogger
            );
        });
    }


    public async ValueTask DisposeAsync()
    {
        if (this.connection != null)
        {
            if (this.ownsConnection)
                this.connection.Dispose();
            this.connection = null;
            this.connecting = null;
        }
    }
}
