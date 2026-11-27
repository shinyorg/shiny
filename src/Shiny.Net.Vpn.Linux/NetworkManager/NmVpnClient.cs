using Tmds.DBus.Protocol;
using MessageNotification = Tmds.DBus.Protocol.Notification<Tmds.DBus.Protocol.Message>;
using VpnStateNotification = Tmds.DBus.Protocol.Notification<Shiny.Net.Vpn.NetworkManager.NmVpnStateChange?>;

namespace Shiny.Net.Vpn.NetworkManager;


/// <summary>A <c>VpnStateChanged</c> signal - the VPN connection's state and why it moved there.</summary>
internal readonly record struct NmVpnStateChange(string Path, uint State, uint Reason);


/// <summary>One of NetworkManager's active connections.</summary>
internal sealed record NmActiveConnection(string Path, uint State);


/// <summary>
/// The signal subscriptions <see cref="LinuxVpnManager"/> listens with, and the connection they live on.
/// </summary>
/// <remarks>
/// A match rule dies with its connection, so <see cref="Lost"/> - the <c>DisconnectedAsync</c> of the
/// exact connection the watches were added on - tells the owner when to subscribe again.
/// </remarks>
internal sealed class NmWatch(IDisposable[] subscriptions, Task<Exception?> lost) : IDisposable
{
    /// <summary>Completes with the reason (null when there is none) once the connection drops.</summary>
    public Task<Exception?> Lost => lost;

    public void Dispose()
    {
        foreach (var subscription in subscriptions)
        {
            try
            {
                subscription.Dispose();
            }
            catch
            {
                // a handle on a dead connection has nothing left to release
            }
        }
    }
}


/// <summary>Where <see cref="LinuxVpnManager"/> gets its watches from - a seam so re-subscribing can be tested without a bus.</summary>
internal interface INmVpnWatchSource
{
    Task<NmWatch> Watch(Action onPropertiesChanged, Action<NmVpnStateChange> onVpnStateChanged, CancellationToken ct = default);
}


/// <summary>
/// The slice of the NetworkManager D-Bus API a VPN profile needs: saved connections by UUID, the
/// active connections, activate/deactivate, and the VPN state signal.
/// </summary>
/// <remarks>
/// Tmds.DBus.Protocol is codegen-free, so messages are written and read by hand - which is where it
/// fails in ways that compile cleanly. Every rule from the repo's Linux notes applies: readers are only
/// used within the callback that owns the message, and signal handlers check <c>IsCompletion</c>
/// before touching anything else and never throw, because a throwing handler disconnects the whole
/// connection.
/// </remarks>
internal sealed class NmVpnClient : INmVpnWatchSource, IAsyncDisposable
{
    readonly SemaphoreSlim connectLock = new(1, 1);
    DBusConnection? connection;


    public async Task<DBusConnection> GetConnection(CancellationToken ct = default)
    {
        if (this.connection != null)
            return this.connection;

        await this.connectLock.WaitAsync(ct).ConfigureAwait(false);
        try
        {
            if (this.connection == null)
            {
                var address = DBusAddress.System
                    ?? throw new VpnException("No D-Bus system bus address is set - DBUS_SYSTEM_BUS_ADDRESS is empty and the default socket is missing");

                var created = new DBusConnection(address);
                try
                {
                    await created.ConnectAsync().ConfigureAwait(false);
                }
                catch
                {
                    created.Dispose();
                    throw;
                }
                this.connection = created;
                _ = this.ForgetWhenDisconnected(created);
            }
            return this.connection;
        }
        finally
        {
            this.connectLock.Release();
        }
    }


    /// <summary>
    /// A dead connection fails every call made on it, so once the bus drops it is forgotten and the
    /// next call connects again rather than the manager staying broken until the process restarts.
    /// </summary>
    async Task ForgetWhenDisconnected(DBusConnection conn)
    {
        try
        {
            await conn.DisconnectedAsync().ConfigureAwait(false);
        }
        catch
        {
            // the reason does not matter here - only that it is gone
        }
        Interlocked.CompareExchange(ref this.connection, null, conn);
    }


    /// <summary>The saved connection with this UUID, or null if NetworkManager has none.</summary>
    public async Task<string?> FindConnection(string uuid, CancellationToken ct = default)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        var writer = conn.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: NmConstants.Service,
            path: NmConstants.SettingsPath,
            @interface: NmConstants.SettingsInterface,
            member: "GetConnectionByUuid",
            signature: "s"
        );
        writer.WriteString(uuid);

        try
        {
            return await conn.CallMethodAsync(
                writer.CreateMessage(),
                static (Message reply, object? _) => reply.GetBodyReader().ReadObjectPathAsString()
            ).ConfigureAwait(false);
        }
        catch (DBusErrorReplyException ex) when (ex.ErrorName == NmConstants.InvalidConnectionError)
        {
            return null;
        }
    }


    /// <summary>Saves a new connection to disk and returns its path. Goes through polkit.</summary>
    public async Task<string> AddConnection(NmConnectionSettings settings, CancellationToken ct = default)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        var writer = conn.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: NmConstants.Service,
            path: NmConstants.SettingsPath,
            @interface: NmConstants.SettingsInterface,
            member: "AddConnection",
            signature: "a{sa{sv}}"
        );
        writer.WriteConnectionSettings(settings);

        return await conn.CallMethodAsync(
            writer.CreateMessage(),
            static (Message reply, object? _) => reply.GetBodyReader().ReadObjectPathAsString()
        ).ConfigureAwait(false);
    }


    /// <summary>Replaces a saved connection's settings and writes them to disk. Goes through polkit.</summary>
    public async Task UpdateConnection(string connectionPath, NmConnectionSettings settings, CancellationToken ct = default)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        var writer = conn.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: NmConstants.Service,
            path: connectionPath,
            @interface: NmConstants.SettingsConnectionInterface,
            member: "Update",
            signature: "a{sa{sv}}"
        );
        writer.WriteConnectionSettings(settings);

        await conn.CallMethodAsync(writer.CreateMessage()).ConfigureAwait(false);
    }


    /// <summary>Deletes a saved connection. Goes through polkit.</summary>
    public async Task DeleteConnection(string connectionPath, CancellationToken ct = default)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        var writer = conn.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: NmConstants.Service,
            path: connectionPath,
            @interface: NmConstants.SettingsConnectionInterface,
            member: "Delete"
        );
        await conn.CallMethodAsync(writer.CreateMessage()).ConfigureAwait(false);
    }


    /// <summary>
    /// Brings a saved connection up and returns the active connection path. A VPN needs no device -
    /// NetworkManager picks the underlying connection - so both device and specific object are null.
    /// </summary>
    public async Task<string> Activate(string connectionPath, CancellationToken ct = default)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        var writer = conn.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: NmConstants.Service,
            path: NmConstants.RootPath,
            @interface: NmConstants.ManagerInterface,
            member: "ActivateConnection",
            signature: "ooo"
        );
        writer.WriteObjectPath(connectionPath);
        writer.WriteObjectPath(NmConstants.NullPath);
        writer.WriteObjectPath(NmConstants.NullPath);

        return await conn.CallMethodAsync(
            writer.CreateMessage(),
            static (Message reply, object? _) => reply.GetBodyReader().ReadObjectPathAsString()
        ).ConfigureAwait(false);
    }


    public async Task Deactivate(string activeConnectionPath, CancellationToken ct = default)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        var writer = conn.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: NmConstants.Service,
            path: NmConstants.RootPath,
            @interface: NmConstants.ManagerInterface,
            member: "DeactivateConnection",
            signature: "o"
        );
        writer.WriteObjectPath(activeConnectionPath);

        await conn.CallMethodAsync(writer.CreateMessage()).ConfigureAwait(false);
    }


    /// <summary>
    /// The active connection for the saved connection with this UUID, or null when it is down.
    /// </summary>
    /// <remarks>
    /// Active connections are short-lived objects - one can vanish between listing it and reading it,
    /// which is a connection going down rather than an error.
    /// </remarks>
    public async Task<NmActiveConnection?> FindActive(string uuid, CancellationToken ct = default)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        var paths = await GetProperty(conn, NmConstants.RootPath, NmConstants.ManagerInterface, "ActiveConnections", static (Message reply, object? _) =>
        {
            var reader = reply.GetBodyReader();
            reader.ReadSignature();
            return reader.ReadArrayOfObjectPath().Select(x => x.ToString()).ToArray();
        }).ConfigureAwait(false);

        foreach (var path in paths)
        {
            try
            {
                var activeUuid = await GetProperty(conn, path, NmConstants.ActiveConnectionInterface, "Uuid", static (Message reply, object? _) =>
                {
                    var reader = reply.GetBodyReader();
                    reader.ReadSignature();
                    return reader.ReadString();
                }).ConfigureAwait(false);

                if (activeUuid == uuid)
                {
                    var state = await GetProperty(conn, path, NmConstants.ActiveConnectionInterface, "State", static (Message reply, object? _) =>
                    {
                        var reader = reply.GetBodyReader();
                        reader.ReadSignature();
                        return reader.ReadUInt32();
                    }).ConfigureAwait(false);

                    return new NmActiveConnection(path, state);
                }
            }
            catch (DBusErrorReplyException)
            {
                // deactivated between the listing and the read
            }
        }
        return null;
    }


    /// <summary>
    /// Adds both VPN watches on one connection and hands back that connection's disconnect, so the
    /// caller knows when the watches are dead.
    /// </summary>
    public async Task<NmWatch> Watch(Action onPropertiesChanged, Action<NmVpnStateChange> onVpnStateChanged, CancellationToken ct = default)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);
        var lost = WhenLost(conn);

        var properties = await WatchPropertiesChanged(conn, onPropertiesChanged).ConfigureAwait(false);
        try
        {
            var vpnState = await WatchVpnStateChanged(conn, onVpnStateChanged).ConfigureAwait(false);
            return new NmWatch([properties, vpnState], lost);
        }
        catch
        {
            properties.Dispose();
            throw;
        }
    }


    static async Task<Exception?> WhenLost(DBusConnection conn)
    {
        try
        {
            return await conn.DisconnectedAsync().ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            return ex;
        }
    }


    /// <summary>
    /// Watches every <c>PropertiesChanged</c> NetworkManager emits, which is how active connections
    /// appearing, changing state and going away show up.
    /// </summary>
    /// <remarks>
    /// Unfiltered by path: the active connection's path is new on every activation, so a path filter
    /// would miss the very change worth hearing about. The caller de-duplicates.
    /// </remarks>
    static async Task<IDisposable> WatchPropertiesChanged(DBusConnection conn, Action onChanged)
    {
        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Sender = NmConstants.Service,
            Interface = NmConstants.PropertiesInterface,
            Member = "PropertiesChanged"
        };

        return await conn.AddMatchAsync(
            rule,
            static (Message msg, object? _) => msg,
            static (MessageNotification n) =>
            {
                // IsCompletion first: on a value notification, reading Exception throws, and a handler
                // that throws makes Tmds disconnect the whole connection
                if (n.IsCompletion)
                    return;

                try
                {
                    ((Action)n.State!).Invoke();
                }
                catch
                {
                    // the callback only schedules a re-read; nothing here may reach Tmds
                }
            },
            emitOnCapturedContext: false,
            ObserverFlags.None,
            onChanged
        ).ConfigureAwait(false);
    }


    /// <summary>
    /// Watches <c>VpnStateChanged</c> on every VPN connection - the only signal that carries the reason
    /// a VPN failed (bad secrets, timeout, plugin missing).
    /// </summary>
    static async Task<IDisposable> WatchVpnStateChanged(DBusConnection conn, Action<NmVpnStateChange> onChanged)
    {
        var rule = new MatchRule
        {
            Type = MessageType.Signal,
            Sender = NmConstants.Service,
            Interface = NmConstants.VpnConnectionInterface,
            Member = "VpnStateChanged"
        };

        return await conn.AddMatchAsync(
            rule,
            // parsed while the message is still ours; a malformed body becomes null rather than an
            // exception, which Tmds would turn into a completion that ends the subscription
            static (Message msg, object? _) =>
            {
                try
                {
                    var reader = msg.GetBodyReader();
                    var state = reader.ReadUInt32();
                    var reason = reader.ReadUInt32();
                    return (NmVpnStateChange?)new NmVpnStateChange(msg.PathAsString ?? String.Empty, state, reason);
                }
                catch
                {
                    return null;
                }
            },
            static (VpnStateNotification n) =>
            {
                if (n.IsCompletion)
                    return;

                try
                {
                    if (n.Value is { } change)
                        ((Action<NmVpnStateChange>)n.State!).Invoke(change);
                }
                catch
                {
                    // nothing here may reach Tmds
                }
            },
            emitOnCapturedContext: false,
            ObserverFlags.None,
            onChanged
        ).ConfigureAwait(false);
    }


    static Task<T> GetProperty<T>(DBusConnection conn, string path, string @interface, string property, MessageValueReader<T> read)
    {
        var writer = conn.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: NmConstants.Service,
            path: path,
            @interface: NmConstants.PropertiesInterface,
            member: "Get",
            signature: "ss"
        );
        writer.WriteString(@interface);
        writer.WriteString(property);

        return conn.CallMethodAsync(writer.CreateMessage(), read);
    }


    public ValueTask DisposeAsync()
    {
        this.connection?.Dispose();
        this.connection = null;
        return ValueTask.CompletedTask;
    }
}
