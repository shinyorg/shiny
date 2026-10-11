using Microsoft.Extensions.Logging;
using Tmds.DBus.Protocol;

namespace Shiny.ScreenRecorder.Portal;


/// <summary>What the compositor handed back after the user picked something to share.</summary>
internal sealed record ScreenCastStream(uint NodeId, int? Width, int? Height);


/// <summary>
/// A client for <c>org.freedesktop.portal.ScreenCast</c> on the session bus.
/// </summary>
/// <remarks>
/// <para>This is how screen capture works on modern Linux. There is no API that hands an app the
/// framebuffer: it asks the portal, the compositor runs its own picker, the user chooses, and the
/// app is given a PipeWire node id to read from. On Wayland that is the only route, and on X11 with
/// a portal installed it is still the polite one - the user sees what is being shared and can stop
/// it.</para>
/// <para>Deliberately does <b>not</b> call <c>OpenPipeWireRemote</c>. That returns a file
/// descriptor for the PipeWire daemon, which only matters inside a sandbox - and passing an
/// inherited descriptor to a child process is not something .NET's <c>Process</c> can do. An
/// unsandboxed app reaches the same daemon through the socket in <c>XDG_RUNTIME_DIR</c> using just
/// the node id, which is what the encoder is given. The consequence is that Flatpak-sandboxed hosts
/// are not supported.</para>
/// </remarks>
internal sealed class ScreenCastPortal : IAsyncDisposable
{
    readonly ILogger logger;
    readonly PortalRequestWatcher watcher = new();

    DBusConnection? connection;
    string? sessionHandle;
    int tokenCounter;


    public ScreenCastPortal(ILogger logger) => this.logger = logger;


    async Task<DBusConnection> GetConnection(CancellationToken ct)
    {
        if (this.connection == null)
        {
            var address = DBusAddress.Session
                ?? throw new ScreenRecorderException("No D-Bus session bus is available - screen recording on Linux needs a desktop session, not a headless one");

            var created = new DBusConnection(address);
            await created.ConnectAsync().ConfigureAwait(false);
            this.connection = created;

            await this.watcher.Start(created, ct).ConfigureAwait(false);
        }

        return this.connection;
    }


    /// <summary>Whether a ScreenCast portal is actually running on this session bus.</summary>
    public async Task<bool> IsAvailable(CancellationToken ct = default)
    {
        try
        {
            var conn = await this.GetConnection(ct).ConfigureAwait(false);

            // reading a property is the cheapest way to prove both that the portal is on the bus
            // and that it implements ScreenCast - some minimal portals implement only FileChooser
            var writer = conn.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: PortalConstants.Service,
                path: PortalConstants.ObjectPath,
                @interface: "org.freedesktop.DBus.Properties",
                member: "Get",
                signature: "ss"
            );
            writer.WriteString(PortalConstants.ScreenCastInterface);
            writer.WriteString("AvailableSourceTypes");

            var types = await conn.CallMethodAsync(
                writer.CreateMessage(),
                static (Message reply, object? _) =>
                {
                    var reader = reply.GetBodyReader();
                    reader.ReadSignature();
                    return reader.ReadUInt32();
                },
                null
            ).ConfigureAwait(false);

            return types != 0;
        }
        catch (Exception ex)
        {
            this.logger.PortalUnavailable(ex);
            return false;
        }
    }


    /// <summary>
    /// Runs the whole consent flow - create a session, choose the sources, show the picker - and
    /// returns the stream the user picked.
    /// </summary>
    public async Task<ScreenCastStream> Start(bool showCursor, CancellationToken ct)
    {
        var conn = await this.GetConnection(ct).ConfigureAwait(false);

        this.sessionHandle = await this.CreateSession(conn, ct).ConfigureAwait(false);
        await this.SelectSources(conn, showCursor, ct).ConfigureAwait(false);

        return await this.StartCast(conn, ct).ConfigureAwait(false);
    }


    async Task<string> CreateSession(DBusConnection conn, CancellationToken ct)
    {
        var writer = conn.GetMessageWriter();
        this.WriteHeader(writer, "CreateSession", "a{sv}");

        var options = writer.WriteDictionaryStart();
        WriteStringOption(writer, "handle_token", this.NextToken());
        WriteStringOption(writer, "session_handle_token", this.NextToken());
        writer.WriteDictionaryEnd(options);

        var response = await this.Call(conn, writer.CreateMessage(), "CreateSession", ct).ConfigureAwait(false);

        if (!response.Results.TryGetValue("session_handle", out var handle))
            throw new ScreenRecorderException("The portal created a session but did not name it");

        return handle.GetString();
    }


    async Task SelectSources(DBusConnection conn, bool showCursor, CancellationToken ct)
    {
        var writer = conn.GetMessageWriter();
        this.WriteHeader(writer, "SelectSources", "oa{sv}");
        writer.WriteObjectPath(this.sessionHandle!);

        var options = writer.WriteDictionaryStart();
        WriteStringOption(writer, "handle_token", this.NextToken());

        writer.WriteDictionaryEntryStart();
        writer.WriteString("types");
        writer.WriteVariantUInt32(PortalConstants.SourceMonitor | PortalConstants.SourceWindow);

        writer.WriteDictionaryEntryStart();
        writer.WriteString("multiple");
        writer.WriteVariantBool(false);

        // the portal is where the cursor decision is made on Wayland - the compositor composites it
        // into the stream or does not, and nothing downstream can add it back
        writer.WriteDictionaryEntryStart();
        writer.WriteString("cursor_mode");
        writer.WriteVariantUInt32(showCursor ? PortalConstants.CursorEmbedded : PortalConstants.CursorHidden);

        writer.WriteDictionaryEnd(options);

        await this.Call(conn, writer.CreateMessage(), "SelectSources", ct).ConfigureAwait(false);
    }


    async Task<ScreenCastStream> StartCast(DBusConnection conn, CancellationToken ct)
    {
        var writer = conn.GetMessageWriter();
        this.WriteHeader(writer, "Start", "osa{sv}");
        writer.WriteObjectPath(this.sessionHandle!);

        // no parent window: this package has no handle on the app's toplevel, so the picker is
        // shown unparented rather than modal to a window it cannot identify
        writer.WriteString(String.Empty);

        var options = writer.WriteDictionaryStart();
        WriteStringOption(writer, "handle_token", this.NextToken());
        writer.WriteDictionaryEnd(options);

        var response = await this.Call(conn, writer.CreateMessage(), "Start", ct).ConfigureAwait(false);

        if (!response.Results.TryGetValue("streams", out var streams) || streams.Count == 0)
            throw new ScreenRecorderException("The portal reported no stream to record");

        return ParseStream(streams.GetItem(0));
    }


    // one stream comes back as (u node_id, a{sv} properties); the properties may carry a "size"
    // (ii) which saves guessing the capture resolution
    static ScreenCastStream ParseStream(VariantValue stream)
    {
        var nodeId = stream.GetItem(0).GetUInt32();
        int? width = null;
        int? height = null;

        try
        {
            var properties = stream.GetItem(1).GetDictionary<string, VariantValue>();

            if (properties.TryGetValue("size", out var size) && size.Count == 2)
            {
                width = (int)size.GetItem(0).GetInt32();
                height = (int)size.GetItem(1).GetInt32();
            }
        }
        catch (Exception)
        {
            // the size hint is optional and its exact shape varies between portal implementations;
            // the encoder falls back to whatever PipeWire negotiates
        }

        return new ScreenCastStream(nodeId, width, height);
    }


    async Task<PortalResponse> Call(DBusConnection conn, MessageBuffer message, string method, CancellationToken ct)
    {
        var requestPath = await conn.CallMethodAsync(
            message,
            static (Message reply, object? _) => reply.GetBodyReader().ReadObjectPathAsString(),
            null
        ).ConfigureAwait(false);

        var response = await this.watcher.Wait(requestPath, ct).ConfigureAwait(false);

        return response.Code switch
        {
            PortalConstants.ResponseSuccess => response,
            PortalConstants.ResponseCancelled => throw new ScreenRecorderPermissionException($"The user cancelled the screen sharing picker ({method})"),
            _ => throw new ScreenRecorderException($"The desktop portal refused the screen cast ({method})")
        };
    }


    void WriteHeader(MessageWriter writer, string member, string signature)
        => writer.WriteMethodCallHeader(
            destination: PortalConstants.Service,
            path: PortalConstants.ObjectPath,
            @interface: PortalConstants.ScreenCastInterface,
            member: member,
            signature: signature
        );


    static void WriteStringOption(MessageWriter writer, string key, string value)
    {
        writer.WriteDictionaryEntryStart();
        writer.WriteString(key);
        writer.WriteVariantString(value);
    }


    // the token becomes part of the request object path, so it must be unique per call and contain
    // only characters legal in a D-Bus path element
    string NextToken() => $"shiny{Environment.ProcessId}_{Interlocked.Increment(ref this.tokenCounter)}";


    /// <summary>Closes the portal session, which stops the compositor streaming.</summary>
    public async Task Close(CancellationToken ct = default)
    {
        if (this.connection == null || this.sessionHandle == null)
            return;

        try
        {
            var writer = this.connection.GetMessageWriter();
            writer.WriteMethodCallHeader(
                destination: PortalConstants.Service,
                path: this.sessionHandle,
                @interface: "org.freedesktop.portal.Session",
                member: "Close"
            );

            await this.connection.CallMethodAsync(writer.CreateMessage()).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.logger.PortalCloseFailed(ex);
        }
        finally
        {
            this.sessionHandle = null;
        }
    }


    public async ValueTask DisposeAsync()
    {
        await this.Close().ConfigureAwait(false);
        await this.watcher.DisposeAsync().ConfigureAwait(false);
        this.connection?.Dispose();
        this.connection = null;
    }
}
