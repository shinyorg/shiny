using System.Collections.Concurrent;
using System.Runtime.CompilerServices;
using System.Threading.Channels;
using Microsoft.Extensions.Logging;
using Shiny.Bluetooth.Bluez;
using Shiny.Bluetooth.Infrastructure;
using Tmds.DBus.Protocol;

namespace Shiny.Bluetooth;


/// <summary>
/// Classic Bluetooth on Linux through BlueZ over D-Bus.
/// </summary>
/// <remarks>
/// Connections go through <c>ProfileManager1</c>: BlueZ does the SDP lookup, the RFCOMM connect or
/// accept and the security checks, then hands this process the connected socket (see
/// <see cref="BluezProfile"/>). Only the default adapter, <c>hci0</c>, is used.
/// </remarks>
public class LinuxBluetoothManager(ILogger<LinuxBluetoothManager> logger) : IBluetoothManager, IBluetoothDeviceOperations
{
    readonly string adapterPath = BluezConstants.DefaultAdapterPath;
    readonly SemaphoreSlim profileLock = new(1, 1);
    readonly ConcurrentDictionary<string, string> clientProfiles = new();
    readonly ConcurrentDictionary<(string Profile, string Device), TaskCompletionSource<BluezFileDescriptor>> pendingConnects = new();
    readonly ConcurrentDictionary<(string Profile, string Device), RfcommStream> openStreams = new();
    DBusConnection? connection;
    Task? connecting;
    int nextProfileId;


    public BluetoothCapabilities Capabilities =>
        BluetoothCapabilities.Discovery |
        BluetoothCapabilities.Pairing |
        BluetoothCapabilities.Connect |
        BluetoothCapabilities.Listen |
        BluetoothCapabilities.Discoverable;


    async Task<DBusConnection> GetConnection(CancellationToken ct)
    {
        if (this.connection == null)
        {
            this.connection = new DBusConnection(DBusAddress.System!);
            this.connecting = this.connection.ConnectAsync().AsTask();
        }
        // every caller waits on the one connect, so a caller that cancels stops waiting without
        // abandoning the connection for the others
        await this.connecting!.WaitAsync(ct).ConfigureAwait(false);
        return this.connection;
    }


    public async Task<AccessState> RequestAccess(CancellationToken ct = default)
    {
        try
        {
            var conn = await this.GetConnection(ct).ConfigureAwait(false);
            var powered = await GetProperty(conn, this.adapterPath, BluezConstants.AdapterInterface, "Powered", ct).ConfigureAwait(false);
            return powered.GetBool() ? AccessState.Available : AccessState.Disabled;
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            logger.LogDebug(ex, "No usable BlueZ adapter at {Path}", this.adapterPath);
            return AccessState.NotSupported;
        }
    }


    public async Task<IReadOnlyList<IBluetoothDevice>> GetPairedDevices(CancellationToken ct = default)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        var devices = await this.GetManagedDevices(conn, ct).ConfigureAwait(false);
        return devices
            .Select(x => x.Properties)
            .Where(x => IsClassic(x) && x.TryGetValue("Paired", out var paired) && paired.GetBool())
            .Select(IBluetoothDevice (x) => this.ToDevice(x))
            .ToList();
    }


    public async Task<IBluetoothDevice> GetDevice(string address, CancellationToken ct = default)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        var path = BluezConstants.DevicePath(this.adapterPath, address);

        // an address BlueZ has no object for comes back bare - Connect then reports that BlueZ does not know it
        return await this.TryGetDeviceProperties(conn, path, ct).ConfigureAwait(false) is { } properties
            ? this.ToDevice(properties)
            : new BluetoothDevice(this, address);
    }


    public async IAsyncEnumerable<BluetoothDiscoveryResult> Discover([EnumeratorCancellation] CancellationToken ct = default)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        var heard = Channel.CreateUnbounded<(string Path, Dictionary<string, VariantValue>? Properties)>();
        var context = new DiscoveryContext(this.adapterPath, heard.Writer, logger);
        var subscriptions = new List<IDisposable>();

        try
        {
            // a device BlueZ has never seen arrives as a new object...
            subscriptions.Add(await conn.AddMatchAsync(
                new MatchRule
                {
                    Type = MessageType.Signal,
                    Interface = BluezConstants.ObjectManagerInterface,
                    Member = "InterfacesAdded"
                },
                static (Message message, object? _) => message,
                static (Notification<Message> n) =>
                {
                    // IsCompletion first: on a value notification reading Exception throws, and a handler
                    // that throws disconnects the whole connection
                    if (n.IsCompletion)
                        return;

                    ((DiscoveryContext)n.State!).OnInterfacesAdded(n.Value);
                },
                emitOnCapturedContext: false,
                ObserverFlags.None,
                context
            ).ConfigureAwait(false));

            // ...and one it already knows only reports a fresh RSSI as the inquiry hears it
            subscriptions.Add(await conn.AddMatchAsync(
                new MatchRule
                {
                    Type = MessageType.Signal,
                    Interface = BluezConstants.PropertiesInterface,
                    Member = "PropertiesChanged",
                    Arg0 = BluezConstants.DeviceInterface
                },
                static (Message message, object? _) => message,
                static (Notification<Message> n) =>
                {
                    if (n.IsCompletion)
                        return;

                    ((DiscoveryContext)n.State!).OnPropertiesChanged(n.Value);
                },
                emitOnCapturedContext: false,
                ObserverFlags.None,
                context
            ).ConfigureAwait(false));

            await this.SetDiscoveryFilter(conn, ct).ConfigureAwait(false);
            await conn.CallAsync(conn.CreateMethodCall(this.adapterPath, BluezConstants.AdapterInterface, "StartDiscovery"), ct).ConfigureAwait(false);
        }
        catch
        {
            foreach (var subscription in subscriptions)
                subscription.Dispose();
            throw;
        }

        // BlueZ inquires until told to stop, so the inquiry gets the same window the other platforms' have
        using var window = CancellationTokenSource.CreateLinkedTokenSource(ct);
        window.CancelAfter(IBluetoothManager.InquiryDuration);

        try
        {
            var seen = new HashSet<string>();
            var reader = heard.Reader;
            while (await WaitToRead(reader, window.Token).ConfigureAwait(false))
            {
                while (reader.TryRead(out var item))
                {
                    var properties = item.Properties ?? await this.TryGetDeviceProperties(conn, item.Path, window.Token).ConfigureAwait(false);
                    if (properties != null && IsClassic(properties) && properties.ContainsKey("Address"))
                    {
                        var device = this.ToDevice(properties);
                        if (seen.Add(device.Address))
                            yield return new BluetoothDiscoveryResult(device, properties.TryGetValue("RSSI", out var rssi) ? rssi.GetInt16() : null);
                    }
                }
            }
            ct.ThrowIfCancellationRequested();
        }
        finally
        {
            foreach (var subscription in subscriptions)
                subscription.Dispose();

            await this.StopDiscovery(conn).ConfigureAwait(false);
        }
    }


    async Task SetDiscoveryFilter(DBusConnection conn, CancellationToken ct)
    {
        var writer = conn.GetMessageWriter();
        MessageBuffer message;
        try
        {
            writer.WriteMethodCallHeader(
                destination: BluezConstants.Service,
                path: this.adapterPath,
                @interface: BluezConstants.AdapterInterface,
                member: "SetDiscoveryFilter",
                signature: "a{sv}"
            );
            writer.WriteDictionary(new Dictionary<string, VariantValue>
            {
                ["Transport"] = VariantValue.String("bredr")
            });
            message = writer.CreateMessage();
        }
        finally
        {
            writer.Dispose();
        }
        await conn.CallAsync(message, ct).ConfigureAwait(false);
    }


    async Task StopDiscovery(DBusConnection conn)
    {
        try
        {
            await conn.CallAsync(conn.CreateMethodCall(this.adapterPath, BluezConstants.AdapterInterface, "StopDiscovery"), CancellationToken.None).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            // NotReady/Failed when the adapter went down mid-inquiry - nothing left to stop
            logger.LogDebug(ex, "StopDiscovery failed");
        }
    }


    // the inquiry window running out is the normal end of a discovery, not a cancellation
    static async Task<bool> WaitToRead<T>(ChannelReader<T> reader, CancellationToken window)
    {
        try
        {
            return await reader.WaitToReadAsync(window).ConfigureAwait(false);
        }
        catch (OperationCanceledException)
        {
            return false;
        }
    }


    async Task<Dictionary<string, VariantValue>?> TryGetDeviceProperties(DBusConnection conn, string path, CancellationToken ct)
    {
        try
        {
            return await GetAllProperties(conn, path, BluezConstants.DeviceInterface, ct).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            // the device object can vanish between the signal and this read
            logger.LogDebug(ex, "Could not read {Path}", path);
            return null;
        }
        catch (OperationCanceledException)
        {
            return null;
        }
    }


    async Task<bool> IBluetoothDeviceOperations.Pair(BluetoothDevice device, CancellationToken ct)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        var path = BluezConstants.DevicePath(this.adapterPath, device.Address);

        try
        {
            var paired = await GetProperty(conn, path, BluezConstants.DeviceInterface, "Paired", ct).ConfigureAwait(false);
            if (paired.GetBool())
                return true;

            await conn.CallAsync(conn.CreateMethodCall(path, BluezConstants.DeviceInterface, "Pair"), ct).ConfigureAwait(false);
        }
        catch (DBusErrorReplyException ex) when (ex.ErrorName == "org.bluez.Error.AlreadyExists")
        {
            return true;
        }
        catch (DBusErrorReplyException ex) when (IsUnknownObject(ex))
        {
            throw UnknownDevice(device);
        }
        catch (DBusErrorReplyException ex)
        {
            // AuthenticationFailed / Rejected / Canceled / Timeout - including no agent to answer a prompt
            logger.LogDebug("Pairing {Address} failed: {Error} {Message}", device.Address, ex.ErrorName, ex.ErrorMessage);
            return false;
        }

        // trusted devices reconnect and use our profiles without BlueZ asking an agent to authorize them
        try
        {
            await conn.CallAsync(conn.CreateSetPropertyCall(path, BluezConstants.DeviceInterface, "Trusted", VariantValue.Bool(true)), ct).ConfigureAwait(false);
        }
        catch (DBusErrorReplyException ex)
        {
            logger.LogDebug("Could not trust {Address}: {Message}", device.Address, ex.ErrorMessage);
        }
        return true;
    }


    async Task<BluetoothConnection> IBluetoothDeviceOperations.Connect(BluetoothDevice device, Guid serviceUuid, bool secure, CancellationToken ct)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        var devicePath = BluezConstants.DevicePath(this.adapterPath, device.Address);
        var profilePath = await this.EnsureClientProfile(conn, serviceUuid, secure, ct).ConfigureAwait(false);

        var key = (profilePath, devicePath);
        var tcs = new TaskCompletionSource<BluezFileDescriptor>(TaskCreationOptions.RunContinuationsAsynchronously);
        if (!this.pendingConnects.TryAdd(key, tcs))
            throw new BluetoothConnectionException($"A connection to {serviceUuid} on {device.Address} is already being opened");

        BluezFileDescriptor fd;
        try
        {
            // BlueZ answers NewConnection on our profile with the socket - sometimes before this call returns
            await conn.CallAsync(conn.CreateMethodCall(devicePath, BluezConstants.DeviceInterface, "ConnectProfile", serviceUuid.ToString()), ct).ConfigureAwait(false);
            fd = await tcs.Task.WaitAsync(ct).ConfigureAwait(false);
        }
        catch (DBusErrorReplyException ex) when (IsUnknownObject(ex))
        {
            throw UnknownDevice(device);
        }
        catch (DBusErrorReplyException ex)
        {
            throw new BluetoothConnectionException($"Could not connect to {serviceUuid} on {device.Address} - {ex.ErrorName}: {ex.ErrorMessage}", ex);
        }
        finally
        {
            this.pendingConnects.TryRemove(key, out _);
        }

        var remote = await this.TryGetDeviceProperties(conn, devicePath, ct).ConfigureAwait(false) is { } properties
            ? this.ToDevice(properties)
            : device;

        return new BluetoothConnection(remote, serviceUuid, this.Track(key, fd));
    }


    async Task<string> EnsureClientProfile(DBusConnection conn, Guid serviceUuid, bool secure, CancellationToken ct)
    {
        var cacheKey = $"{serviceUuid:N}{(secure ? "s" : "i")}";
        await this.profileLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (this.clientProfiles.TryGetValue(cacheKey, out var existing))
                return existing;

            var path = $"{BluezConstants.ProfileRootPath}/client{Interlocked.Increment(ref this.nextProfileId)}";
            var profile = new BluezProfile(
                path,
                (device, fd) =>
                {
                    if (!this.pendingConnects.TryGetValue((path, device), out var pending) || !pending.TrySetResult(fd))
                        fd.Dispose(); // the caller gave up before BlueZ finished
                },
                device => this.OnDisconnectRequested(path, device),
                () => this.ForgetClientProfile(cacheKey, path)
            );

            await this.RegisterProfile(conn, profile, serviceUuid, new Dictionary<string, VariantValue>
            {
                ["Role"] = VariantValue.String("client"),
                ["AutoConnect"] = VariantValue.Bool(false),
                ["RequireAuthentication"] = VariantValue.Bool(secure),
                ["RequireAuthorization"] = VariantValue.Bool(false)
            }, ct).ConfigureAwait(false);

            this.clientProfiles[cacheKey] = path;
            return path;
        }
        finally
        {
            this.profileLock.Release();
        }
    }


    void ForgetClientProfile(string cacheKey, string path)
    {
        // BlueZ released it (bluetoothd restarted) - the next Connect registers a fresh one
        this.clientProfiles.TryRemove(cacheKey, out _);
        this.connection?.RemoveMethodHandler(path);
    }


    public async Task<BluetoothListener> Listen(Guid serviceUuid, string serviceName, bool secure = true, CancellationToken ct = default)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        var channel = RfcommSocket.FindFreeChannel();
        var path = $"{BluezConstants.ProfileRootPath}/server{Interlocked.Increment(ref this.nextProfileId)}";

        BluetoothListener? listener = null;
        var profile = new BluezProfile(
            path,
            (device, fd) =>
            {
                var stream = this.Track((path, device), fd);
                listener!.Enqueue(new BluetoothConnection(new BluetoothDevice(this, AddressFromPath(device)), serviceUuid, stream));
            },
            device => this.OnDisconnectRequested(path, device),
            () => listener?.Fault(new BluetoothException("BlueZ released the profile - the adapter went away or bluetoothd restarted"))
        );

        listener = new BluetoothListener(serviceUuid, serviceName, async () =>
        {
            try
            {
                await conn.CallAsync(this.CreateUnregisterProfile(conn, path), CancellationToken.None).ConfigureAwait(false);
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "UnregisterProfile {Path} failed", path);
            }
            conn.RemoveMethodHandler(path);
        });

        await this.RegisterProfile(conn, profile, serviceUuid, new Dictionary<string, VariantValue>
        {
            ["Name"] = VariantValue.String(serviceName),
            ["Role"] = VariantValue.String("server"),
            ["Channel"] = VariantValue.UInt16(channel),
            ["AutoConnect"] = VariantValue.Bool(false),
            ["RequireAuthentication"] = VariantValue.Bool(secure),
            ["RequireAuthorization"] = VariantValue.Bool(false)
        }, ct).ConfigureAwait(false);

        logger.LogDebug("Published {Uuid} on RFCOMM channel {Channel}", serviceUuid, channel);
        return listener;
    }


    public async Task<bool> RequestDiscoverable(TimeSpan duration, CancellationToken ct = default)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        try
        {
            await conn.CallAsync(conn.CreateSetPropertyCall(this.adapterPath, BluezConstants.AdapterInterface, "DiscoverableTimeout", VariantValue.UInt32((uint)duration.TotalSeconds)), ct).ConfigureAwait(false);
            await conn.CallAsync(conn.CreateSetPropertyCall(this.adapterPath, BluezConstants.AdapterInterface, "Discoverable", VariantValue.Bool(true)), ct).ConfigureAwait(false);
            return true;
        }
        catch (DBusErrorReplyException ex)
        {
            throw new BluetoothPermissionException($"BlueZ refused to make the adapter discoverable - {ex.ErrorName}: {ex.ErrorMessage}");
        }
    }


    async Task RegisterProfile(DBusConnection conn, BluezProfile profile, Guid serviceUuid, Dictionary<string, VariantValue> options, CancellationToken ct)
    {
        conn.AddMethodHandler(profile);

        var writer = conn.GetMessageWriter();
        MessageBuffer message;
        try
        {
            writer.WriteMethodCallHeader(
                destination: BluezConstants.Service,
                path: BluezConstants.RootPath,
                @interface: BluezConstants.ProfileManagerInterface,
                member: "RegisterProfile",
                signature: "osa{sv}"
            );
            writer.WriteObjectPath(profile.Path);
            writer.WriteString(serviceUuid.ToString());
            writer.WriteDictionary(options);
            message = writer.CreateMessage();
        }
        finally
        {
            writer.Dispose();
        }

        try
        {
            await conn.CallAsync(message, ct).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            conn.RemoveMethodHandler(profile.Path);
            if (ex is DBusErrorReplyException reply)
                throw new BluetoothException($"BlueZ would not register {serviceUuid} - {reply.ErrorName}: {reply.ErrorMessage}", ex);
            throw;
        }
    }


    MessageBuffer CreateUnregisterProfile(DBusConnection conn, string path)
    {
        var writer = conn.GetMessageWriter();
        try
        {
            writer.WriteMethodCallHeader(
                destination: BluezConstants.Service,
                path: BluezConstants.RootPath,
                @interface: BluezConstants.ProfileManagerInterface,
                member: "UnregisterProfile",
                signature: "o"
            );
            writer.WriteObjectPath(path);
            return writer.CreateMessage();
        }
        finally
        {
            writer.Dispose();
        }
    }


    RfcommStream Track((string Profile, string Device) key, BluezFileDescriptor fd)
    {
        RfcommStream? stream = null;
        stream = new RfcommStream(fd, () => this.openStreams.TryRemove(new KeyValuePair<(string, string), RfcommStream>(key, stream!)));
        this.openStreams[key] = stream;
        return stream;
    }


    void OnDisconnectRequested(string profilePath, string devicePath)
    {
        // BlueZ hands the socket back to us to close - end the stream so the reader sees end of stream
        if (this.openStreams.TryGetValue((profilePath, devicePath), out var stream))
            stream.ShutdownFromRemote();
    }


    async Task<List<(string Path, Dictionary<string, VariantValue> Properties)>> GetManagedDevices(DBusConnection conn, CancellationToken ct)
    {
        var message = conn.CreateMethodCall("/", BluezConstants.ObjectManagerInterface, "GetManagedObjects");
        var adapter = this.adapterPath;
        return await conn.CallAsync(
            message,
            (Message reply, object? _) => DbusExtensions.ReadDevices(reply, adapter),
            ct
        ).ConfigureAwait(false);
    }


    static Task<VariantValue> GetProperty(DBusConnection conn, string path, string @interface, string property, CancellationToken ct)
    {
        var writer = conn.GetMessageWriter();
        MessageBuffer message;
        try
        {
            writer.WriteMethodCallHeader(
                destination: BluezConstants.Service,
                path: path,
                @interface: BluezConstants.PropertiesInterface,
                member: "Get",
                signature: "ss"
            );
            writer.WriteString(@interface);
            writer.WriteString(property);
            message = writer.CreateMessage();
        }
        finally
        {
            writer.Dispose();
        }

        // ReadVariantValue reads the variant's own signature - no ReadSignature before it
        return conn.CallAsync(message, static (Message reply, object? _) => reply.GetBodyReader().ReadVariantValue(), ct);
    }


    static Task<Dictionary<string, VariantValue>> GetAllProperties(DBusConnection conn, string path, string @interface, CancellationToken ct)
        => conn.CallAsync(
            conn.CreateMethodCall(path, BluezConstants.PropertiesInterface, "GetAll", @interface),
            static (Message reply, object? _) => reply.GetBodyReader().ReadDictionaryOfStringToVariantValue(),
            ct
        );


    // a BR/EDR device reports its Class of Device; an LE-only one has none
    static bool IsClassic(Dictionary<string, VariantValue> properties) => properties.ContainsKey("Class");


    BluetoothDevice ToDevice(Dictionary<string, VariantValue> properties)
        => new(this, properties["Address"].GetString())
        {
            Name = properties.TryGetValue("Name", out var name) ? name.GetString() : null,
            Paired = properties.TryGetValue("Paired", out var paired) && paired.GetBool(),
            ClassOfDevice = properties.TryGetValue("Class", out var cod) ? cod.GetUInt32() : null
        };


    internal static string AddressFromPath(string devicePath)
    {
        var index = devicePath.LastIndexOf("/dev_", StringComparison.Ordinal);
        if (index < 0)
            throw new ArgumentException($"'{devicePath}' is not a BlueZ device path", nameof(devicePath));

        return BluetoothAddress.Normalize(devicePath[(index + 5)..].Replace('_', ':'));
    }


    static bool IsUnknownObject(DBusErrorReplyException ex)
        => ex.ErrorName is "org.freedesktop.DBus.Error.UnknownObject" or "org.freedesktop.DBus.Error.UnknownMethod";


    static BluetoothConnectionException UnknownDevice(BluetoothDevice device)
        => new($"BlueZ does not know {device.Address} - pair with it or find it with Discover first");


    sealed class DiscoveryContext(string adapterPath, ChannelWriter<(string, Dictionary<string, VariantValue>?)> heard, ILogger logger)
    {
        // a handler that throws disconnects the D-Bus connection, so every parse failure stops here
        public void OnInterfacesAdded(Message message)
        {
            try
            {
                var reader = message.GetBodyReader();
                var path = reader.ReadObjectPathAsString();
                if (!path.StartsWith(adapterPath + "/", StringComparison.Ordinal))
                    return;

                var interfacesEnd = reader.ReadArrayStart(DBusType.DictEntry);
                while (reader.HasNext(interfacesEnd))
                {
                    var iface = reader.ReadString();
                    var properties = reader.ReadDictionaryOfStringToVariantValue();
                    if (iface == BluezConstants.DeviceInterface)
                        heard.TryWrite((path, properties));
                }
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Could not read InterfacesAdded");
            }
        }


        public void OnPropertiesChanged(Message message)
        {
            try
            {
                var path = message.PathAsString;
                if (path == null || !path.StartsWith(adapterPath + "/", StringComparison.Ordinal))
                    return;

                var reader = message.GetBodyReader();
                reader.ReadString(); // interface - the match rule's arg0 already pinned it to Device1
                var changed = reader.ReadDictionaryOfStringToVariantValue();
                if (changed.ContainsKey("RSSI"))
                    heard.TryWrite((path, null));
            }
            catch (Exception ex)
            {
                logger.LogDebug(ex, "Could not read PropertiesChanged");
            }
        }
    }
}
