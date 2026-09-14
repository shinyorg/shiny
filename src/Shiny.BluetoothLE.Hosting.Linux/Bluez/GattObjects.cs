using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Tmds.DBus.Protocol;

namespace Shiny.BluetoothLE.Hosting.Bluez;


/// <summary>
/// What the exported GATT objects need from the hosting manager.
/// </summary>
internal interface IBluezGattHost
{
    ILogger Logger { get; }

    /// <summary>
    /// The central behind a request, updated with the MTU BlueZ reported for it.
    /// </summary>
    Peripheral GetPeripheral(string? devicePath, ushort mtu);

    /// <summary>
    /// BlueZ called StartNotify or StopNotify on a characteristic.
    /// </summary>
    void SetNotifying(GattCharacteristic characteristic, bool notifying);
}


/// <summary>
/// The root of the exported GATT application.
/// </summary>
/// <remarks>
/// BlueZ discovers an external GATT server by calling <c>ObjectManager.GetManagedObjects</c> on the path passed to
/// <c>GattManager1.RegisterApplication</c>, once, while that call is in flight. It ignores objects added afterwards
/// and drops the whole application if one is removed, which is why the manager re-registers on every change.
/// </remarks>
internal sealed class GattApplicationObject(string path, Func<IReadOnlyList<GattService>> getServices) : IPathMethodHandler
{
    public string Path { get; } = path;
    public bool HandlesChildPaths => false;


    public ValueTask HandleMethodAsync(MethodContext context)
    {
        var request = context.Request;
        if (request.InterfaceAsString == BluezConstants.ObjectManagerInterface && request.MemberAsString == "GetManagedObjects")
            this.ReplyManagedObjects(context);
        else if (context.IsDBusIntrospectRequest)
            DbusObject.ReplyIntrospect(context, IntrospectXml);
        else
            context.ReplyUnknownMethodError();

        return default;
    }


    void ReplyManagedObjects(MethodContext context)
    {
        var writer = context.CreateReplyWriter("a{oa{sa{sv}}}");
        try
        {
            var objects = writer.WriteDictionaryStart();
            foreach (var service in getServices())
            {
                WriteObject(
                    ref writer,
                    service.ObjectPath!,
                    BluezConstants.GattServiceInterface,
                    GattServiceObject.PropertyNames,
                    (ref MessageWriter w, string name) => GattServiceObject.WriteProperty(ref w, service, name)
                );

                foreach (var characteristic in service.NativeCharacteristics)
                {
                    WriteObject(
                        ref writer,
                        characteristic.ObjectPath!,
                        BluezConstants.GattCharacteristicInterface,
                        GattCharacteristicObject.PropertyNames,
                        (ref MessageWriter w, string name) => GattCharacteristicObject.WriteProperty(ref w, characteristic, name)
                    );
                }
            }
            writer.WriteDictionaryEnd(objects);
            context.Reply(writer.CreateMessage());
        }
        finally
        {
            writer.Dispose();
        }
    }


    static void WriteObject(ref MessageWriter writer, string path, string interfaceName, IReadOnlyList<string> names, DbusObject.PropertyWriter writeOne)
    {
        writer.WriteDictionaryEntryStart();
        writer.WriteObjectPath(path);

        var interfaces = writer.WriteDictionaryStart();
        writer.WriteDictionaryEntryStart();
        writer.WriteString(interfaceName);
        DbusObject.WriteProperties(ref writer, names, writeOne);
        writer.WriteDictionaryEnd(interfaces);
    }


    const string IntrospectXml =
        """
        <!DOCTYPE node PUBLIC "-//freedesktop//DTD D-BUS Object Introspection 1.0//EN" "http://www.freedesktop.org/standards/dbus/1.0/introspect.dtd">
        <node>
          <interface name="org.freedesktop.DBus.ObjectManager">
            <method name="GetManagedObjects">
              <arg name="objects" type="a{oa{sa{sv}}}" direction="out"/>
            </method>
          </interface>
        </node>
        """;
}


/// <summary>
/// An exported <c>org.bluez.GattService1</c>.
/// </summary>
internal sealed class GattServiceObject(GattService service) : IPathMethodHandler
{
    internal static readonly string[] PropertyNames = ["UUID", "Primary"];

    public string Path { get; } = service.ObjectPath!;
    public bool HandlesChildPaths => false;


    public ValueTask HandleMethodAsync(MethodContext context)
    {
        if (context.IsPropertiesInterfaceRequest)
        {
            DbusObject.HandleProperties(
                context,
                BluezConstants.GattServiceInterface,
                PropertyNames,
                (ref MessageWriter w, string name) => WriteProperty(ref w, service, name)
            );
        }
        else if (context.IsDBusIntrospectRequest)
        {
            DbusObject.ReplyIntrospect(context, IntrospectXml);
        }
        else
        {
            context.ReplyUnknownMethodError();
        }
        return default;
    }


    internal static bool WriteProperty(ref MessageWriter writer, GattService service, string name)
    {
        switch (name)
        {
            case "UUID":
                writer.WriteVariantString(BluezGatt.NormalizeUuid(service.Uuid));
                return true;

            case "Primary":
                writer.WriteVariantBool(service.Primary);
                return true;

            default:
                return false;
        }
    }


    const string IntrospectXml =
        """
        <!DOCTYPE node PUBLIC "-//freedesktop//DTD D-BUS Object Introspection 1.0//EN" "http://www.freedesktop.org/standards/dbus/1.0/introspect.dtd">
        <node>
          <interface name="org.bluez.GattService1">
            <property name="UUID" type="s" access="read"/>
            <property name="Primary" type="b" access="read"/>
          </interface>
          <interface name="org.freedesktop.DBus.Properties">
            <method name="Get"><arg name="interface" type="s" direction="in"/><arg name="name" type="s" direction="in"/><arg name="value" type="v" direction="out"/></method>
            <method name="GetAll"><arg name="interface" type="s" direction="in"/><arg name="properties" type="a{sv}" direction="out"/></method>
          </interface>
        </node>
        """;
}


/// <summary>
/// An exported <c>org.bluez.GattCharacteristic1</c> - the object BlueZ calls into for reads, writes and
/// notification state.
/// </summary>
internal sealed class GattCharacteristicObject(GattCharacteristic characteristic, IBluezGattHost host) : IPathMethodHandler
{
    internal static readonly string[] PropertyNames = ["UUID", "Service", "Flags", "Notifying"];

    public string Path { get; } = characteristic.ObjectPath!;
    public bool HandlesChildPaths => false;


    public ValueTask HandleMethodAsync(MethodContext context)
    {
        var request = context.Request;
        if (request.InterfaceAsString == BluezConstants.GattCharacteristicInterface)
        {
            switch (request.MemberAsString)
            {
                case "ReadValue":
                    return this.ReadValue(context);

                case "WriteValue":
                    return this.WriteValue(context);

                case "StartNotify":
                    this.SetNotifying(context, true);
                    break;

                case "StopNotify":
                    this.SetNotifying(context, false);
                    break;

                default:
                    context.ReplyUnknownMethodError();
                    break;
            }
        }
        else if (context.IsPropertiesInterfaceRequest)
        {
            DbusObject.HandleProperties(
                context,
                BluezConstants.GattCharacteristicInterface,
                PropertyNames,
                (ref MessageWriter w, string name) => WriteProperty(ref w, characteristic, name)
            );
        }
        else if (context.IsDBusIntrospectRequest)
        {
            DbusObject.ReplyIntrospect(context, IntrospectXml);
        }
        else
        {
            context.ReplyUnknownMethodError();
        }
        return default;
    }


    internal static bool WriteProperty(ref MessageWriter writer, GattCharacteristic characteristic, string name)
    {
        switch (name)
        {
            case "UUID":
                writer.WriteVariantString(BluezGatt.NormalizeUuid(characteristic.Uuid));
                return true;

            case "Service":
                writer.WriteVariantObjectPath(characteristic.ServicePath!);
                return true;

            case "Flags":
                // a variant on the wire is its signature followed by the value - there is no WriteVariant helper for arrays
                writer.WriteSignature("as");
                writer.WriteArray(BluezGatt.ToFlags(characteristic));
                return true;

            case "Notifying":
                writer.WriteVariantBool(characteristic.IsNotifying);
                return true;

            default:
                return false;
        }
    }


    void SetNotifying(MethodContext context, bool notifying)
    {
        var canNotify =
            characteristic.NativeProperties.HasFlag(CharacteristicProperties.Notify) ||
            characteristic.NativeProperties.HasFlag(CharacteristicProperties.Indicate);

        if (!canNotify)
        {
            context.ReplyError(BluezConstants.ErrorNotSupported, "Characteristic does not support notifications");
            return;
        }

        // reply before running subscription hooks, so user code cannot hold BlueZ's call open
        DbusObject.ReplyEmpty(context);
        host.SetNotifying(characteristic, notifying);
    }


    async ValueTask ReadValue(MethodContext context)
    {
        var handler = characteristic.OnReadHandler;
        if (handler == null)
        {
            context.ReplyError(BluezConstants.ErrorNotSupported, "Characteristic does not support read");
            return;
        }

        var options = ReadOptions(context.Request);
        var request = new ReadRequest(characteristic, host.GetPeripheral(options.DevicePath, options.Mtu), options.Offset);

        GattResult result;
        try
        {
            result = await handler(request).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            host.Logger.LogWarning(ex, "Read handler for characteristic {Uuid} threw", characteristic.Uuid);
            context.ReplyError(BluezConstants.ErrorFailed, ex.Message);
            return;
        }

        if (result.Status == GattState.Success)
            ReplyBytes(context, result.Data ?? []);
        else
            context.ReplyError(BluezGatt.ToErrorName(result.Status), result.Status.ToString());
    }


    async ValueTask WriteValue(MethodContext context)
    {
        var handler = characteristic.OnWriteHandler;
        if (handler == null)
        {
            context.ReplyError(BluezConstants.ErrorNotSupported, "Characteristic does not support write");
            return;
        }

        var (value, options) = ReadWriteArguments(context.Request);

        // "command" is a write without response. BlueZ still waits for this call to return, but there is
        // no ATT response to put a status in
        var replyNeeded = options.Type != "command";
        var replied = 0;

        void Respond(GattState state)
        {
            // the D-Bus reply IS the GATT response, so it goes out the moment the handler responds - a
            // request/response handler notifies straight after, and the central expects them in that order
            if (Interlocked.Exchange(ref replied, 1) == 1)
                return;

            if (state == GattState.Success)
                DbusObject.ReplyEmpty(context);
            else
                context.ReplyError(BluezGatt.ToErrorName(state), state.ToString());
        }

        var request = new WriteRequest(
            characteristic,
            host.GetPeripheral(options.DevicePath, options.Mtu),
            value,
            options.Offset,
            replyNeeded,
            Respond
        );

        try
        {
            await handler(request).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            host.Logger.LogWarning(ex, "Write handler for characteristic {Uuid} threw", characteristic.Uuid);
            if (Interlocked.Exchange(ref replied, 1) == 0)
                context.ReplyError(BluezConstants.ErrorFailed, ex.Message);

            return;
        }

        // a handler that never called Respond succeeded
        Respond(GattState.Success);
    }


    // Reader is a ref struct and cannot live in an async method, so the bodies are parsed in these sync helpers

    static GattRequestOptions ReadOptions(Message request)
        => GattRequestOptions.From(request.GetBodyReader().ReadDictionaryOfStringToVariantValue());


    static (byte[] Value, GattRequestOptions Options) ReadWriteArguments(Message request)
    {
        var reader = request.GetBodyReader();
        var value = reader.ReadArrayOfByte();
        return (value, GattRequestOptions.From(reader.ReadDictionaryOfStringToVariantValue()));
    }


    static void ReplyBytes(MethodContext context, byte[] data)
    {
        var writer = context.CreateReplyWriter("ay");
        try
        {
            writer.WriteArray(data);
            context.Reply(writer.CreateMessage());
        }
        finally
        {
            writer.Dispose();
        }
    }


    const string IntrospectXml =
        """
        <!DOCTYPE node PUBLIC "-//freedesktop//DTD D-BUS Object Introspection 1.0//EN" "http://www.freedesktop.org/standards/dbus/1.0/introspect.dtd">
        <node>
          <interface name="org.bluez.GattCharacteristic1">
            <method name="ReadValue"><arg name="options" type="a{sv}" direction="in"/><arg name="value" type="ay" direction="out"/></method>
            <method name="WriteValue"><arg name="value" type="ay" direction="in"/><arg name="options" type="a{sv}" direction="in"/></method>
            <method name="StartNotify"/>
            <method name="StopNotify"/>
            <property name="UUID" type="s" access="read"/>
            <property name="Service" type="o" access="read"/>
            <property name="Flags" type="as" access="read"/>
            <property name="Notifying" type="b" access="read"/>
          </interface>
          <interface name="org.freedesktop.DBus.Properties">
            <method name="Get"><arg name="interface" type="s" direction="in"/><arg name="name" type="s" direction="in"/><arg name="value" type="v" direction="out"/></method>
            <method name="GetAll"><arg name="interface" type="s" direction="in"/><arg name="properties" type="a{sv}" direction="out"/></method>
            <signal name="PropertiesChanged"><arg name="interface" type="s"/><arg name="changed" type="a{sv}"/><arg name="invalidated" type="as"/></signal>
          </interface>
        </node>
        """;
}
