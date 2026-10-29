using Tmds.DBus.Protocol;

namespace Shiny.Bluetooth.Bluez;


/// <summary>
/// A D-Bus object implementing <c>org.bluez.Profile1</c>.
/// </summary>
/// <remarks>
/// <para>
/// BlueZ owns classic connections: an app registers a profile (a UUID plus this object's path) with
/// <c>ProfileManager1</c>, and BlueZ does the SDP work, the RFCOMM connect or accept and the security
/// checks, then calls <c>NewConnection</c> <b>back into this process</b> with the connected socket as a
/// file descriptor. That one callback serves both directions - a server profile receives accepted
/// connections, and a client profile receives the result of <c>Device1.ConnectProfile</c>.
/// </para>
/// <para>
/// Handlers here run on Tmds' dispatch and must never throw - a throwing handler disconnects the whole
/// D-Bus connection, ending every other profile and discovery with it.
/// </para>
/// </remarks>
internal sealed class BluezProfile(
    string path,
    Action<string, BluezFileDescriptor> onConnection,
    Action<string> onDisconnectRequested,
    Action onRelease
) : IPathMethodHandler
{
    const string IntrospectXml = """
        <node>
          <interface name="org.bluez.Profile1">
            <method name="Release"/>
            <method name="NewConnection">
              <arg name="device" type="o" direction="in"/>
              <arg name="fd" type="h" direction="in"/>
              <arg name="fd_properties" type="a{sv}" direction="in"/>
            </method>
            <method name="RequestDisconnection">
              <arg name="device" type="o" direction="in"/>
            </method>
          </interface>
        </node>
        """;


    public string Path { get; } = path;
    public bool HandlesChildPaths => false;


    public ValueTask HandleMethodAsync(MethodContext context)
    {
        var request = context.Request;
        switch (request.InterfaceAsString)
        {
            case BluezConstants.ProfileInterface when request.MemberAsString == "NewConnection":
                this.HandleNewConnection(context);
                break;

            case BluezConstants.ProfileInterface when request.MemberAsString == "RequestDisconnection":
            {
                var device = request.GetBodyReader().ReadObjectPathAsString();
                ReplyEmpty(context);
                onDisconnectRequested(device);
                break;
            }

            case BluezConstants.ProfileInterface when request.MemberAsString == "Release":
                // reply before the callback, so a handler that re-registers does not deadlock on this call
                ReplyEmpty(context);
                onRelease();
                break;

            case BluezConstants.IntrospectableInterface when request.MemberAsString == "Introspect":
            {
                var writer = context.CreateReplyWriter("s");
                try
                {
                    writer.WriteString(IntrospectXml);
                    context.Reply(writer.CreateMessage());
                }
                finally
                {
                    writer.Dispose();
                }
                break;
            }

            default:
                context.ReplyUnknownMethodError();
                break;
        }
        return default;
    }


    void HandleNewConnection(MethodContext context)
    {
        string device;
        BluezFileDescriptor? fd;
        try
        {
            var reader = context.Request.GetBodyReader();
            device = reader.ReadObjectPathAsString();
            fd = reader.ReadHandle<BluezFileDescriptor>();
        }
        catch (Exception ex)
        {
            context.ReplyError("org.bluez.Error.Rejected", ex.Message);
            return;
        }

        if (fd == null || fd.IsInvalid)
        {
            context.ReplyError("org.bluez.Error.Rejected", "No file descriptor was passed");
            return;
        }

        ReplyEmpty(context);
        try
        {
            onConnection(device, fd);
        }
        catch
        {
            // the consumer is gone - drop the connection rather than let the exception reach Tmds
            fd.Dispose();
        }
    }


    static void ReplyEmpty(MethodContext context)
    {
        var writer = context.CreateReplyWriter("");
        try
        {
            context.Reply(writer.CreateMessage());
        }
        finally
        {
            writer.Dispose();
        }
    }
}
