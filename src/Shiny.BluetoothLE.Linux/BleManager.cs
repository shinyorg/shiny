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

namespace Shiny.BluetoothLE;


public class BleManager : IBleManager, IAsyncDisposable
{
    readonly IOperationQueue operations;
    readonly ILogger<BleManager> logger;
    readonly ILogger<Peripheral> peripheralLogger;
    Connection? connection;
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


    async Task<Connection> GetConnectionAsync(CancellationToken ct = default)
    {
        if (this.connection == null)
        {
            this.connection = new Connection(Address.System!);
            await this.connection.ConnectAsync().ConfigureAwait(false);
        }
        return this.connection;
    }


    async Task<BluezAdapter> GetAdapterAsync(CancellationToken ct = default)
    {
        if (this.adapter == null)
        {
            var conn = await this.GetConnectionAsync(ct).ConfigureAwait(false);
            this.adapter = new BluezAdapter(conn);
        }
        return this.adapter;
    }


    public IObservable<AccessState> RequestAccess() => Observable.FromAsync(async ct =>
    {
        try
        {
            var adapter = await this.GetAdapterAsync(ct).ConfigureAwait(false);
            var powered = await adapter.GetPoweredAsync(ct).ConfigureAwait(false);
            return powered ? AccessState.Available : AccessState.Disabled;
        }
        catch
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
                catch
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
                    (Exception? ex, Message msg, object? _, object? state) =>
                    {
                        if (ex != null) return;
                        try
                        {
                            var ctx = (ScanContext)state!;
                            ctx.Manager.ProcessInterfacesAdded(msg, ctx.Observer);
                        }
                        catch (Exception exc)
                        {
                            ((ScanContext)state!).Manager.logger.BluezError(exc, "Error processing InterfacesAdded");
                        }
                    },
                    ObserverFlags.None,
                    readerState: null,
                    handlerState: new ScanContext(this, ob)
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
                    (Exception? ex, Message msg, object? _, object? state) =>
                    {
                        if (ex != null) return;
                        try
                        {
                            var ctx = (ScanContext)state!;
                            ctx.Manager.ProcessPropertiesChanged(msg, ctx.Observer);
                        }
                        catch (Exception exc)
                        {
                            ((ScanContext)state!).Manager.logger.BluezError(exc, "Error processing PropertiesChanged");
                        }
                    },
                    ObserverFlags.None,
                    readerState: null,
                    handlerState: new ScanContext(this, ob)
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
        if (!objectPath.StartsWith(BluezConstants.DefaultAdapterPath))
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
                            reader.ReadSignature();
                            reader.ReadVariantValue();
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
                    reader.ReadSignature();
                    reader.ReadVariantValue();
                }
            }
        }
    }


    void ProcessPropertiesChanged(Message message, IObserver<ScanResult> ob)
    {
        var path = message.PathAsString;
        if (path == null || !path.StartsWith(BluezConstants.DefaultAdapterPath + "/dev_"))
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
                    reader.ReadSignature();
                    reader.ReadVariantValue();
                    break;
            }
        }

        if (hasUpdate && this.IsScanning)
        {
            var ad = AdvertisementData.FromDeviceProperties(peripheral.Name, null, 0, null, null);
            ob.OnNext(new ScanResult(peripheral, rssi, ad));
        }
    }


    async Task EmitExistingDevicesAsync(Connection conn, IObserver<ScanResult> ob, ScanConfig? config, CancellationToken ct)
    {
        var msg = conn.CreateMethodCall(
            BluezConstants.Service,
            "/",
            BluezConstants.ObjectManagerInterface,
            "GetManagedObjects"
        );

        // GetManagedObjects returns a{oa{sa{sv}}} which we parse via MessageValueReader
        var devices = await conn.CallMethodAsync(
            msg,
            static (Message reply, object? _) =>
            {
                var result = new List<(string path, string? name, string? address, short rssi, string[]? uuids, short txPower)>();
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

                        if (iface == BluezConstants.DeviceInterface && path.StartsWith(BluezConstants.DefaultAdapterPath))
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
                                        reader.ReadSignature();
                                        reader.ReadVariantValue();
                                        break;
                                }
                            }
                        }
                        else
                        {
                            while (reader.HasNext(propsEnd))
                            {
                                reader.ReadString();
                                reader.ReadSignature();
                                reader.ReadVariantValue();
                            }
                        }
                    }

                    if (isDevice && address != null)
                        result.Add((path, name, address, rssi, uuids, txPower));
                }

                return result;
            }
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
            this.connection.Dispose();
            this.connection = null;
        }
    }
}
