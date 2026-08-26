using System;
using System.Collections.Generic;
using System.Linq;
using System.Reactive;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.BluetoothLE.Bluez;
using Shiny.BluetoothLE.Intrastructure;
using Tmds.DBus.Protocol;
using MessageNotification = Tmds.DBus.Protocol.Notification<Tmds.DBus.Protocol.Message>;

namespace Shiny.BluetoothLE;


public partial class Peripheral : IPeripheral
{
    readonly DBusConnection connection;
    readonly BluezDevice device;
    readonly IOperationQueue operations;
    readonly ILogger logger;
    readonly Subject<ConnectionState> connSubj = new();
    readonly Subject<BleException> connFailedSubj = new();
    IDisposable? autoReconnectSub;


    internal Peripheral(
        DBusConnection connection,
        string objectPath,
        string uuid,
        string? name,
        IOperationQueue operations,
        ILogger logger)
    {
        this.connection = connection;
        this.device = new BluezDevice(connection, objectPath);
        this.Uuid = uuid;
        this.Name = name;
        this.operations = operations;
        this.logger = logger;
    }


    internal string ObjectPath => this.device.ObjectPath;
    public string Uuid { get; }
    public string? Name { get; internal set; }
    public int Mtu => 512 - BleConstants.AttHeaderSize; // BlueZ negotiates MTU internally; 512 is a safe assumed ATT MTU
    public ConnectionState Status { get; internal set; } = ConnectionState.Disconnected;


    public void Connect(ConnectionConfig? config = null)
    {
        var arc = config?.AutoConnect ?? true;
        if (arc)
        {
            this.autoReconnectSub = this
                .WhenStatusChanged()
                .Where(x => x == ConnectionState.Disconnected)
                .Skip(1)
                .Subscribe(_ => Task.Run(async () =>
                {
                    try
                    {
                        await this.device.ConnectAsync().ConfigureAwait(false);
                    }
                    catch
                    {
                        // auto-reconnect failed, will retry on next disconnect event
                    }
                }));
        }

        _ = Task.Run(async () =>
        {
            try
            {
                this.Status = ConnectionState.Connecting;
                this.connSubj.OnNext(ConnectionState.Connecting);
                await this.device.ConnectAsync().ConfigureAwait(false);
                this.Status = ConnectionState.Connected;
                this.connSubj.OnNext(ConnectionState.Connected);
            }
            catch (Exception ex)
            {
                this.Status = ConnectionState.Disconnected;
                this.connSubj.OnNext(ConnectionState.Disconnected);
                this.connFailedSubj.OnNext(new BleException(ex.Message));
            }
        });
    }


    public void CancelConnection()
    {
        this.autoReconnectSub?.Dispose();
        this.autoReconnectSub = null;

        _ = Task.Run(async () =>
        {
            try
            {
                this.Status = ConnectionState.Disconnecting;
                this.connSubj.OnNext(ConnectionState.Disconnecting);
                await this.device.DisconnectAsync().ConfigureAwait(false);
                this.Status = ConnectionState.Disconnected;
                this.connSubj.OnNext(ConnectionState.Disconnected);
            }
            catch (Exception ex)
            {
                this.logger.BluezError(ex, "Error disconnecting from device");
                this.Status = ConnectionState.Disconnected;
                this.connSubj.OnNext(ConnectionState.Disconnected);
            }
        });
    }


    public IObservable<ConnectionState> WhenStatusChanged() => Observable.Create<ConnectionState>(ob =>
    {
        ob.OnNext(this.Status);
        return this.connSubj.Subscribe(ob.OnNext);
    });


    public IObservable<BleException> WhenConnectionFailed() => this.connFailedSubj;
    public IObservable<Unit> WhenServicesChanged() => Observable.Never<Unit>();


    public IObservable<int> ReadRssi() => Observable.FromAsync(async ct =>
    {
        var rssi = await this.device.GetRssiAsync(ct).ConfigureAwait(false);
        return (int)rssi;
    });


    public IObservable<BleServiceInfo> GetService(string serviceUuid) => this
        .GetServices()
        .Select(services =>
        {
            var svc = services.FirstOrDefault(x => x.Uuid.Equals(serviceUuid, StringComparison.OrdinalIgnoreCase));
            return svc ?? throw new InvalidOperationException($"Service '{serviceUuid}' not found");
        });


    public IObservable<IReadOnlyList<BleServiceInfo>> GetServices() => this.operations.QueueToObservable(async ct =>
    {
        if (this.Status != ConnectionState.Connected)
            throw new InvalidOperationException("GATT is not connected");

        // Wait for services resolved
        for (var i = 0; i < 50; i++)
        {
            if (await this.device.GetServicesResolvedAsync(ct).ConfigureAwait(false))
                break;
            await Task.Delay(100, ct).ConfigureAwait(false);
        }

        var services = await this.DiscoverGattServicesAsync(ct).ConfigureAwait(false);
        return (IReadOnlyList<BleServiceInfo>)services.Select(s => new BleServiceInfo(s.uuid)).ToList();
    });


    public IObservable<BleCharacteristicInfo> GetCharacteristic(string serviceUuid, string characteristicUuid) => this
        .GetCharacteristics(serviceUuid)
        .Select(chars =>
        {
            var ch = chars.FirstOrDefault(x => x.Uuid.Equals(characteristicUuid, StringComparison.OrdinalIgnoreCase));
            return ch ?? throw new InvalidOperationException($"Characteristic '{characteristicUuid}' not found in service '{serviceUuid}'");
        });


    public IObservable<IReadOnlyList<BleCharacteristicInfo>> GetCharacteristics(string serviceUuid) => this.operations.QueueToObservable(async ct =>
    {
        if (this.Status != ConnectionState.Connected)
            throw new InvalidOperationException("GATT is not connected");

        var services = await this.DiscoverGattServicesAsync(ct).ConfigureAwait(false);
        var service = services.FirstOrDefault(s => s.uuid.Equals(serviceUuid, StringComparison.OrdinalIgnoreCase));
        if (service.path == null)
            throw new InvalidOperationException($"Service '{serviceUuid}' not found");

        var chars = await this.DiscoverGattCharacteristicsAsync(service.path, ct).ConfigureAwait(false);
        var serviceInfo = new BleServiceInfo(serviceUuid);

        return (IReadOnlyList<BleCharacteristicInfo>)chars
            .Select(c => new BleCharacteristicInfo(serviceInfo, c.uuid, c.notifying, c.properties))
            .ToList();
    });


    public IObservable<BleCharacteristicResult> ReadCharacteristic(string serviceUuid, string characteristicUuid) => this.operations.QueueToObservable(async ct =>
    {
        var charPath = await this.FindCharacteristicPathAsync(serviceUuid, characteristicUuid, ct).ConfigureAwait(false);
        var ch = new BluezGattCharacteristic(this.connection, charPath);
        var data = await ch.ReadValueAsync(ct: ct).ConfigureAwait(false);
        var flags = await ch.GetFlagsAsync(ct).ConfigureAwait(false);

        return new BleCharacteristicResult(
            new BleCharacteristicInfo(
                new BleServiceInfo(serviceUuid),
                characteristicUuid,
                false,
                BluezGattCharacteristic.FlagsToProperties(flags)
            ),
            BleCharacteristicEvent.Read,
            data
        );
    });


    public IObservable<BleCharacteristicResult> WriteCharacteristic(string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true)
        => this.operations.QueueToObservable(async ct =>
    {
        var charPath = await this.FindCharacteristicPathAsync(serviceUuid, characteristicUuid, ct).ConfigureAwait(false);
        var ch = new BluezGattCharacteristic(this.connection, charPath);
        await ch.WriteValueAsync(data, withResponse, ct).ConfigureAwait(false);
        var flags = await ch.GetFlagsAsync(ct).ConfigureAwait(false);

        return new BleCharacteristicResult(
            new BleCharacteristicInfo(
                new BleServiceInfo(serviceUuid),
                characteristicUuid,
                false,
                BluezGattCharacteristic.FlagsToProperties(flags)
            ),
            withResponse ? BleCharacteristicEvent.Write : BleCharacteristicEvent.WriteWithoutResponse,
            data
        );
    });


    record NotifyContext(string ServiceUuid, string CharacteristicUuid, IObserver<BleCharacteristicResult> Observer);

    public IObservable<BleCharacteristicResult> NotifyCharacteristic(string serviceUuid, string characteristicUuid, bool useIndicationsIfAvailable = true)
        => Observable.Create<BleCharacteristicResult>(ob =>
    {
        var cts = new CancellationTokenSource();
        IDisposable? signalSub = null;

        _ = Task.Run(async () =>
        {
            try
            {
                var charPath = await this.FindCharacteristicPathAsync(serviceUuid, characteristicUuid, cts.Token).ConfigureAwait(false);
                var ch = new BluezGattCharacteristic(this.connection, charPath);
                var flags = await ch.GetFlagsAsync(cts.Token).ConfigureAwait(false);
                var charInfo = new BleCharacteristicInfo(
                    new BleServiceInfo(serviceUuid),
                    characteristicUuid,
                    true,
                    BluezGattCharacteristic.FlagsToProperties(flags)
                );

                // Watch for PropertiesChanged on the characteristic for Value changes
                var rule = new MatchRule
                {
                    Type = MessageType.Signal,
                    Interface = BluezConstants.PropertiesInterface,
                    Member = "PropertiesChanged",
                    Path = charPath
                };

                signalSub = await this.connection.AddMatchAsync(
                    rule,
                    static (Message msg, object? _) => msg,
                    static (MessageNotification n) =>
                    {
                        if (n.Exception != null) return;
                        var ctx = (NotifyContext)n.State!;

                        var reader = n.Value.GetBodyReader();
                        var iface = reader.ReadString();
                        if (iface != BluezConstants.GattCharacteristicInterface)
                            return;

                        // Read the changed properties dict a{sv}
                        var arrayEnd = reader.ReadArrayStart(DBusType.DictEntry);
                        while (reader.HasNext(arrayEnd))
                        {
                            var propName = reader.ReadString();
                            if (propName == "Value")
                            {
                                var data = reader.ReadByteArrayVariant();
                                var info = new BleCharacteristicInfo(
                                    new BleServiceInfo(ctx.ServiceUuid),
                                    ctx.CharacteristicUuid,
                                    true,
                                    (CharacteristicProperties)0
                                );
                                ctx.Observer.OnNext(new BleCharacteristicResult(info, BleCharacteristicEvent.Notification, data));
                            }
                            else
                            {
                                reader.ReadSignature();
                                reader.ReadVariantValue();
                            }
                        }
                    },
                    emitOnCapturedContext: false,
                    ObserverFlags.None,
                    new NotifyContext(serviceUuid, characteristicUuid, ob)
                ).ConfigureAwait(false);

                await ch.StartNotifyAsync(cts.Token).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                if (!cts.IsCancellationRequested)
                    ob.OnError(ex);
            }
        }, cts.Token);

        return () =>
        {
            cts.Cancel();
            signalSub?.Dispose();
            cts.Dispose();
            // best-effort stop notify
            _ = Task.Run(async () =>
            {
                try
                {
                    var charPath = await this.FindCharacteristicPathAsync(serviceUuid, characteristicUuid, CancellationToken.None).ConfigureAwait(false);
                    var ch = new BluezGattCharacteristic(this.connection, charPath);
                    await ch.StopNotifyAsync().ConfigureAwait(false);
                }
                catch
                {
                    // best effort
                }
            });
        };
    });


    public IObservable<BleCharacteristicInfo> WhenCharacteristicSubscriptionChanged(string serviceUuid, string characteristicUuid)
        => Observable.Never<BleCharacteristicInfo>();


    public IObservable<BleDescriptorInfo> GetDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid) => this
        .GetDescriptors(serviceUuid, characteristicUuid)
        .Select(descs =>
        {
            var desc = descs.FirstOrDefault(x => x.Uuid.Equals(descriptorUuid, StringComparison.OrdinalIgnoreCase));
            return desc ?? throw new InvalidOperationException($"Descriptor '{descriptorUuid}' not found");
        });


    public IObservable<IReadOnlyList<BleDescriptorInfo>> GetDescriptors(string serviceUuid, string characteristicUuid) => this.operations.QueueToObservable(async ct =>
    {
        var charPath = await this.FindCharacteristicPathAsync(serviceUuid, characteristicUuid, ct).ConfigureAwait(false);
        var descriptors = await this.DiscoverGattDescriptorsAsync(charPath, ct).ConfigureAwait(false);
        var serviceInfo = new BleServiceInfo(serviceUuid);
        var charFlags = await new BluezGattCharacteristic(this.connection, charPath).GetFlagsAsync(ct).ConfigureAwait(false);
        var charInfo = new BleCharacteristicInfo(serviceInfo, characteristicUuid, false, BluezGattCharacteristic.FlagsToProperties(charFlags));

        return (IReadOnlyList<BleDescriptorInfo>)descriptors
            .Select(d => new BleDescriptorInfo(charInfo, d.uuid))
            .ToList();
    });


    public IObservable<BleDescriptorResult> ReadDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid) => this.operations.QueueToObservable(async ct =>
    {
        var descPath = await this.FindDescriptorPathAsync(serviceUuid, characteristicUuid, descriptorUuid, ct).ConfigureAwait(false);
        var desc = new BluezGattDescriptor(this.connection, descPath);
        var data = await desc.ReadValueAsync(ct: ct).ConfigureAwait(false);

        var charPath = await this.FindCharacteristicPathAsync(serviceUuid, characteristicUuid, ct).ConfigureAwait(false);
        var charFlags = await new BluezGattCharacteristic(this.connection, charPath).GetFlagsAsync(ct).ConfigureAwait(false);
        var charInfo = new BleCharacteristicInfo(new BleServiceInfo(serviceUuid), characteristicUuid, false, BluezGattCharacteristic.FlagsToProperties(charFlags));

        return new BleDescriptorResult(
            new BleDescriptorInfo(charInfo, descriptorUuid),
            data
        );
    });


    public IObservable<BleDescriptorResult> WriteDescriptor(string serviceUuid, string characteristicUuid, string descriptorUuid, byte[] data) => this.operations.QueueToObservable(async ct =>
    {
        var descPath = await this.FindDescriptorPathAsync(serviceUuid, characteristicUuid, descriptorUuid, ct).ConfigureAwait(false);
        var desc = new BluezGattDescriptor(this.connection, descPath);
        await desc.WriteValueAsync(data, ct).ConfigureAwait(false);

        var charPath = await this.FindCharacteristicPathAsync(serviceUuid, characteristicUuid, ct).ConfigureAwait(false);
        var charFlags = await new BluezGattCharacteristic(this.connection, charPath).GetFlagsAsync(ct).ConfigureAwait(false);
        var charInfo = new BleCharacteristicInfo(new BleServiceInfo(serviceUuid), characteristicUuid, false, BluezGattCharacteristic.FlagsToProperties(charFlags));

        return new BleDescriptorResult(
            new BleDescriptorInfo(charInfo, descriptorUuid),
            data
        );
    });


    internal void ReceiveConnectionChange(bool connected)
    {
        this.Status = connected ? ConnectionState.Connected : ConnectionState.Disconnected;
        this.connSubj.OnNext(this.Status);
        this.logger.DeviceConnectionChanged(this.device.ObjectPath, connected);
    }


    async Task<List<(string path, string uuid)>> DiscoverGattServicesAsync(CancellationToken ct)
    {
        var msg = this.connection.CreateMethodCall(
            BluezConstants.Service,
            "/",
            BluezConstants.ObjectManagerInterface,
            "GetManagedObjects"
        );

        return await this.connection.CallMethodAsync(
            msg,
            (Message reply, object? state) =>
            {
                var devicePath = (string)state!;
                var result = new List<(string path, string uuid)>();
                var reader = reply.GetBodyReader();

                var objectsEnd = reader.ReadArrayStart(DBusType.DictEntry);
                while (reader.HasNext(objectsEnd))
                {
                    var path = (string)reader.ReadObjectPath();
                    var interfacesEnd = reader.ReadArrayStart(DBusType.DictEntry);
                    while (reader.HasNext(interfacesEnd))
                    {
                        var iface = reader.ReadString();
                        var propsEnd = reader.ReadArrayStart(DBusType.DictEntry);

                        if (iface == BluezConstants.GattServiceInterface && path.StartsWith(devicePath))
                        {
                            string? uuid = null;
                            while (reader.HasNext(propsEnd))
                            {
                                var propName = reader.ReadString();
                                if (propName == "UUID")
                                    uuid = reader.ReadStringVariant();
                                else
                                {
                                    reader.ReadSignature();
                                    reader.ReadVariantValue();
                                }
                            }
                            if (uuid != null)
                                result.Add((path, uuid));
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

                return result;
            },
            readerState: this.device.ObjectPath
        ).ConfigureAwait(false);
    }


    async Task<List<(string path, string uuid, bool notifying, CharacteristicProperties properties)>> DiscoverGattCharacteristicsAsync(string servicePath, CancellationToken ct)
    {
        var msg = this.connection.CreateMethodCall(
            BluezConstants.Service,
            "/",
            BluezConstants.ObjectManagerInterface,
            "GetManagedObjects"
        );

        return await this.connection.CallMethodAsync(
            msg,
            (Message reply, object? state) =>
            {
                var svcPath = (string)state!;
                var result = new List<(string path, string uuid, bool notifying, CharacteristicProperties properties)>();
                var reader = reply.GetBodyReader();

                var objectsEnd = reader.ReadArrayStart(DBusType.DictEntry);
                while (reader.HasNext(objectsEnd))
                {
                    var path = (string)reader.ReadObjectPath();
                    var interfacesEnd = reader.ReadArrayStart(DBusType.DictEntry);
                    while (reader.HasNext(interfacesEnd))
                    {
                        var iface = reader.ReadString();
                        var propsEnd = reader.ReadArrayStart(DBusType.DictEntry);

                        if (iface == BluezConstants.GattCharacteristicInterface && path.StartsWith(svcPath))
                        {
                            string? uuid = null;
                            bool notifying = false;
                            string[] flags = Array.Empty<string>();

                            while (reader.HasNext(propsEnd))
                            {
                                var propName = reader.ReadString();
                                switch (propName)
                                {
                                    case "UUID":
                                        uuid = reader.ReadStringVariant();
                                        break;
                                    case "Notifying":
                                        notifying = reader.ReadBoolVariant();
                                        break;
                                    case "Flags":
                                        flags = reader.ReadStringArrayVariant();
                                        break;
                                    default:
                                        reader.ReadSignature();
                                        reader.ReadVariantValue();
                                        break;
                                }
                            }
                            if (uuid != null)
                                result.Add((path, uuid, notifying, BluezGattCharacteristic.FlagsToProperties(flags)));
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

                return result;
            },
            readerState: servicePath
        ).ConfigureAwait(false);
    }


    async Task<List<(string path, string uuid)>> DiscoverGattDescriptorsAsync(string characteristicPath, CancellationToken ct)
    {
        var msg = this.connection.CreateMethodCall(
            BluezConstants.Service,
            "/",
            BluezConstants.ObjectManagerInterface,
            "GetManagedObjects"
        );

        return await this.connection.CallMethodAsync(
            msg,
            (Message reply, object? state) =>
            {
                var charPath = (string)state!;
                var result = new List<(string path, string uuid)>();
                var reader = reply.GetBodyReader();

                var objectsEnd = reader.ReadArrayStart(DBusType.DictEntry);
                while (reader.HasNext(objectsEnd))
                {
                    var path = (string)reader.ReadObjectPath();
                    var interfacesEnd = reader.ReadArrayStart(DBusType.DictEntry);
                    while (reader.HasNext(interfacesEnd))
                    {
                        var iface = reader.ReadString();
                        var propsEnd = reader.ReadArrayStart(DBusType.DictEntry);

                        if (iface == BluezConstants.GattDescriptorInterface && path.StartsWith(charPath))
                        {
                            string? uuid = null;
                            while (reader.HasNext(propsEnd))
                            {
                                var propName = reader.ReadString();
                                if (propName == "UUID")
                                    uuid = reader.ReadStringVariant();
                                else
                                {
                                    reader.ReadSignature();
                                    reader.ReadVariantValue();
                                }
                            }
                            if (uuid != null)
                                result.Add((path, uuid));
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

                return result;
            },
            readerState: characteristicPath
        ).ConfigureAwait(false);
    }


    async Task<string> FindCharacteristicPathAsync(string serviceUuid, string characteristicUuid, CancellationToken ct)
    {
        var services = await this.DiscoverGattServicesAsync(ct).ConfigureAwait(false);
        var service = services.FirstOrDefault(s => s.uuid.Equals(serviceUuid, StringComparison.OrdinalIgnoreCase));
        if (service.path == null)
            throw new InvalidOperationException($"Service '{serviceUuid}' not found");

        var chars = await this.DiscoverGattCharacteristicsAsync(service.path, ct).ConfigureAwait(false);
        var ch = chars.FirstOrDefault(c => c.uuid.Equals(characteristicUuid, StringComparison.OrdinalIgnoreCase));
        if (ch.path == null)
            throw new InvalidOperationException($"Characteristic '{characteristicUuid}' not found in service '{serviceUuid}'");

        return ch.path;
    }


    async Task<string> FindDescriptorPathAsync(string serviceUuid, string characteristicUuid, string descriptorUuid, CancellationToken ct)
    {
        var charPath = await this.FindCharacteristicPathAsync(serviceUuid, characteristicUuid, ct).ConfigureAwait(false);
        var descs = await this.DiscoverGattDescriptorsAsync(charPath, ct).ConfigureAwait(false);
        var desc = descs.FirstOrDefault(d => d.uuid.Equals(descriptorUuid, StringComparison.OrdinalIgnoreCase));
        if (desc.path == null)
            throw new InvalidOperationException($"Descriptor '{descriptorUuid}' not found");

        return desc.path;
    }
}
