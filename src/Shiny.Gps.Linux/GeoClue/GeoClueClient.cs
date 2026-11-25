using Tmds.DBus.Protocol;

namespace Shiny.Locations.GeoClue;


/// <summary>
/// The slice of GeoClue2's D-Bus API a GPS manager needs. Tmds.DBus.Protocol is deliberately codegen-free,
/// so messages are written and read by hand.
/// </summary>
/// <remarks>
/// GeoClue hands one client per connection from <c>GetClient</c>, so the client path is cached for the life
/// of the connection: starting and stopping it again reuses the same object.
/// </remarks>
internal sealed class GeoClueClient : IAsyncDisposable
{
    readonly SemaphoreSlim gate = new(1, 1);
    DBusConnection? connection;
    string? clientPath;


    public async Task<DBusConnection> GetConnection()
    {
        await this.gate.WaitAsync().ConfigureAwait(false);
        try
        {
            if (this.connection == null)
            {
                var address = DBusAddress.System
                    ?? throw new InvalidOperationException("No D-Bus system bus address is set - DBUS_SYSTEM_BUS_ADDRESS is empty and the default socket is missing");

                var created = new DBusConnection(address);
                await created.ConnectAsync().ConfigureAwait(false);
                this.connection = created;
            }
            return this.connection;
        }
        finally
        {
            this.gate.Release();
        }
    }


    /// <summary>GeoClue is D-Bus activated, so it is there if it's activatable or already running.</summary>
    public async Task<bool> IsServiceAvailable()
    {
        var connection = await this.GetConnection().ConfigureAwait(false);
        var activatable = await this.ListNames(connection, "ListActivatableNames").ConfigureAwait(false);
        if (activatable.Contains(GeoClueConstants.Service))
            return true;

        var running = await this.ListNames(connection, "ListNames").ConfigureAwait(false);
        return running.Contains(GeoClueConstants.Service);
    }


    /// <summary>The connection's client, configured for the next start.</summary>
    public async Task<string> GetClient(string desktopId, GeoClueAccuracyLevel level, uint distanceThreshold, uint timeThreshold)
    {
        var connection = await this.GetConnection().ConfigureAwait(false);
        this.clientPath ??= await connection.CallMethodAsync(
            Call(connection, GeoClueConstants.Service, GeoClueConstants.ManagerPath, GeoClueConstants.ManagerInterface, "GetClient"),
            static (Message reply, object? _) => reply.GetBodyReader().ReadObjectPathAsString()
        ).ConfigureAwait(false);

        // the desktop id is what the location agent shows and remembers the answer by - GeoClue refuses a client without one
        await SetProperty(connection, this.clientPath, "DesktopId", w => w.WriteVariantString(desktopId)).ConfigureAwait(false);
        await SetProperty(connection, this.clientPath, "RequestedAccuracyLevel", w => w.WriteVariantUInt32((uint)level)).ConfigureAwait(false);
        await SetProperty(connection, this.clientPath, "DistanceThreshold", w => w.WriteVariantUInt32(distanceThreshold)).ConfigureAwait(false);
        await SetProperty(connection, this.clientPath, "TimeThreshold", w => w.WriteVariantUInt32(timeThreshold)).ConfigureAwait(false);

        return this.clientPath;
    }


    /// <summary>Starts the client - the point at which GeoClue asks its agent whether the app may locate.</summary>
    public async Task Start(string client)
    {
        var connection = await this.GetConnection().ConfigureAwait(false);
        await connection.CallMethodAsync(Call(connection, GeoClueConstants.Service, client, GeoClueConstants.ClientInterface, "Start")).ConfigureAwait(false);
    }


    public async Task Stop(string client)
    {
        var connection = await this.GetConnection().ConfigureAwait(false);
        await connection.CallMethodAsync(Call(connection, GeoClueConstants.Service, client, GeoClueConstants.ClientInterface, "Stop")).ConfigureAwait(false);
    }


    /// <summary>
    /// Watches the client's <c>LocationUpdated(o old, o new)</c> signal, handing over the new location's
    /// path. Called on the D-Bus thread.
    /// </summary>
    public async Task<IDisposable> WatchLocationUpdated(string client, Action<string> onUpdated)
    {
        var connection = await this.GetConnection().ConfigureAwait(false);
        return await connection.WatchSignalAsync(
            GeoClueConstants.Service,
            client,
            GeoClueConstants.ClientInterface,
            "LocationUpdated",
            static (Message message, object? _) =>
            {
                var reader = message.GetBodyReader();
                reader.ReadObjectPathAsString();
                return reader.ReadObjectPathAsString();
            },
            (Notification<string> n) =>
            {
                if (n.HasValue)
                    onUpdated(n.Value);
            },
            ObserverFlags.None,
            emitOnCapturedContext: false
        ).ConfigureAwait(false);
    }


    /// <summary>Reads a Location object's properties into a reading.</summary>
    public async Task<GpsReading> ReadLocation(string location)
    {
        var connection = await this.GetConnection().ConfigureAwait(false);
        var p = await connection.CallMethodAsync(
            Call(connection, GeoClueConstants.Service, location, GeoClueConstants.PropertiesInterface, "GetAll", "s", w => w.WriteString(GeoClueConstants.LocationInterface)),
            static (Message reply, object? _) => reply.GetBodyReader().ReadDictionaryOfStringToVariantValue()
        ).ConfigureAwait(false);

        ulong seconds = 0;
        ulong micros = 0;
        if (p.TryGetValue("Timestamp", out var ts))
        {
            var stamp = Unwrap(ts);
            if (stamp.Type == VariantValueType.Struct && stamp.Count >= 2)
            {
                seconds = Unwrap(stamp.GetItem(0)).GetUInt64();
                micros = Unwrap(stamp.GetItem(1)).GetUInt64();
            }
        }

        return GeoClueMapping.ToReading(
            Double(p, "Latitude", 0),
            Double(p, "Longitude", 0),
            Double(p, "Accuracy", 0),
            Double(p, "Altitude", -double.MaxValue),
            Double(p, "Speed", -1),
            Double(p, "Heading", -1),
            seconds,
            micros,
            DateTimeOffset.UtcNow
        );
    }


    public ValueTask DisposeAsync()
    {
        this.connection?.Dispose();
        this.connection = null;
        this.clientPath = null;
        return ValueTask.CompletedTask;
    }


    Task<string[]> ListNames(DBusConnection connection, string member)
        => connection.CallMethodAsync(
            Call(connection, GeoClueConstants.DBusService, GeoClueConstants.DBusPath, GeoClueConstants.DBusInterface, member),
            static (Message reply, object? _) => reply.GetBodyReader().ReadArrayOfString()
        );


    static Task SetProperty(DBusConnection connection, string client, string property, Action<MessageWriter> writeVariant)
        => connection.CallMethodAsync(Call(connection, GeoClueConstants.Service, client, GeoClueConstants.PropertiesInterface, "Set", "ssv", w =>
        {
            w.WriteString(GeoClueConstants.ClientInterface);
            w.WriteString(property);
            writeVariant(w);
        }));


    static MessageBuffer Call(DBusConnection connection, string destination, string path, string @interface, string member, string? signature = null, Action<MessageWriter>? write = null)
    {
        var writer = connection.GetMessageWriter();
        writer.WriteMethodCallHeader(destination: destination, path: path, @interface: @interface, member: member, signature: signature);
        write?.Invoke(writer);
        return writer.CreateMessage();
    }


    static double Double(Dictionary<string, VariantValue> properties, string name, double fallback)
        => properties.TryGetValue(name, out var value) && Unwrap(value) is { Type: VariantValueType.Double } d
            ? d.GetDouble()
            : fallback;


    /// <summary>A variant's value, looking through any variant wrapped around it.</summary>
    static VariantValue Unwrap(VariantValue value)
        => value.Type == VariantValueType.Variant ? Unwrap(value.GetVariantValue()) : value;
}
