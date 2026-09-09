using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Shiny.BluetoothLE.Hosting.Bluez;


/// <summary>
/// A D-Bus object implementing <c>org.bluez.LEAdvertisement1</c>.
/// </summary>
/// <remarks>
/// <para>
/// BlueZ advertising is inverted compared with every other platform: rather than handing the payload
/// to an API, the application <b>exports an object</b> describing the advertisement and registers its
/// path with <c>LEAdvertisingManager1</c>. BlueZ then calls <b>back into this process</b> - a
/// <c>Properties.GetAll</c> on this object - to read what to broadcast, and calls
/// <c>Release()</c> when it drops the advertisement.
/// </para>
/// <para>
/// So this type is a server, not a client, and it must stay alive and answering for as long as the
/// advertisement is registered.
/// </para>
/// </remarks>
internal sealed class LEAdvertisement(
    string path,
    LEAdvertisementProperties properties,
    Action onRelease
) : IPathMethodHandler
{
    /// <inheritdoc />
    public string Path { get; } = path;

    /// <inheritdoc />
    public bool HandlesChildPaths => false;


    /// <inheritdoc />
    public ValueTask HandleMethodAsync(MethodContext context)
    {
        var request = context.Request;

        switch (request.InterfaceAsString)
        {
            case BluezConstants.PropertiesInterface:
                this.HandleProperties(context);
                break;

            case BluezConstants.LEAdvertisementInterface when request.MemberAsString == "Release":
                // BlueZ calls this when it stops the advertisement for a reason of its own - the
                // adapter powering down, another client taking the slot, or the daemon restarting.
                // The reply must go out before the callback runs, or a handler that tries to
                // re-register deadlocks against this method call.
                ReplyEmpty(context);
                onRelease();
                break;

            case BluezConstants.IntrospectableInterface when request.MemberAsString == "Introspect":
            {
                using var writer = context.CreateReplyWriter("s");
                writer.WriteString(IntrospectXml);
                context.Reply(writer.CreateMessage());
                break;
            }

            default:
                context.ReplyUnknownMethodError();
                break;
        }

        return default;
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


    void HandleProperties(MethodContext context)
    {
        switch (context.Request.MemberAsString)
        {
            case "GetAll":
            {
                // MessageWriter is a ref struct, so it cannot be a `using` variable and passed by
                // ref at the same time - hence the explicit try/finally.
                var writer = context.CreateReplyWriter("a{sv}");
                try
                {
                    this.WriteAll(ref writer);
                    context.Reply(writer.CreateMessage());
                }
                finally
                {
                    writer.Dispose();
                }
                break;
            }

            case "Get":
            {
                var reader = context.Request.GetBodyReader();
                reader.ReadString();                    // interface name - we only have the one
                var name = reader.ReadString();

                var writer = context.CreateReplyWriter("v");
                try
                {
                    if (this.WriteOne(ref writer, name))
                    {
                        context.Reply(writer.CreateMessage());
                    }
                    else
                    {
                        context.ReplyError(
                            "org.freedesktop.DBus.Error.InvalidArgs",
                            $"No such property '{name}'"
                        );
                    }
                }
                finally
                {
                    writer.Dispose();
                }
                break;
            }

            case "Set":
                // every LEAdvertisement1 property is read-only from BlueZ's side
                context.ReplyError(
                    "org.freedesktop.DBus.Error.PropertyReadOnly",
                    "LEAdvertisement1 properties are read-only"
                );
                break;

            default:
                context.ReplyUnknownMethodError();
                break;
        }
    }


    void WriteAll(ref MessageWriter writer)
    {
        var dict = writer.WriteDictionaryStart();

        foreach (var name in properties.PresentPropertyNames())
        {
            writer.WriteDictionaryEntryStart();
            writer.WriteString(name);
            this.WriteOne(ref writer, name);
        }

        writer.WriteDictionaryEnd(dict);
    }


    bool WriteOne(ref MessageWriter writer, string name)
    {
        switch (name)
        {
            case "Type":
                writer.WriteVariantString(properties.Type);
                return true;

            case "LocalName" when properties.LocalName != null:
                writer.WriteVariantString(properties.LocalName);
                return true;

            case "Discoverable" when properties.IsConnectable:
                writer.WriteVariantBool(properties.Discoverable);
                return true;

            case "ServiceUUIDs" when properties.ServiceUuids.Count > 0:
                // a variant on the wire is its signature followed by the value, which is exactly
                // what the WriteVariantXxx helpers do - there just is not one for arrays
                writer.WriteSignature("as");
                writer.WriteArray(properties.ServiceUuids.ToArray());
                return true;

            case "Includes" when properties.Includes.Count > 0:
                writer.WriteSignature("as");
                writer.WriteArray(properties.Includes.ToArray());
                return true;

            case "ServiceData" when properties.ServiceData.Count > 0:
                writer.WriteSignature("a{sv}");
                WriteByteArrayDictionary(ref writer, properties.ServiceData, static (ref MessageWriter w, string key) => w.WriteString(key));
                return true;

            case "ManufacturerData" when properties.ManufacturerData.Count > 0:
                writer.WriteSignature("a{qv}");
                WriteByteArrayDictionary(ref writer, properties.ManufacturerData, static (ref MessageWriter w, ushort key) => w.WriteUInt16(key));
                return true;

            default:
                return false;
        }
    }


    delegate void KeyWriter<in TKey>(ref MessageWriter writer, TKey key);


    static void WriteByteArrayDictionary<TKey>(
        ref MessageWriter writer,
        IReadOnlyDictionary<TKey, byte[]> values,
        KeyWriter<TKey> writeKey
    ) where TKey : notnull
    {
        var dict = writer.WriteDictionaryStart();

        foreach (var pair in values)
        {
            writer.WriteDictionaryEntryStart();
            writeKey(ref writer, pair.Key);
            writer.WriteSignature("ay");
            writer.WriteArray(pair.Value);
        }

        writer.WriteDictionaryEnd(dict);
    }


    // BlueZ does not introspect us, but busctl and d-feet do, and being able to read the exported
    // object back is the difference between five minutes and an afternoon when advertising misbehaves.
    const string IntrospectXml =
        """
        <!DOCTYPE node PUBLIC "-//freedesktop//DTD D-BUS Object Introspection 1.0//EN" "http://www.freedesktop.org/standards/dbus/1.0/introspect.dtd">
        <node>
          <interface name="org.bluez.LEAdvertisement1">
            <method name="Release"/>
            <property name="Type" type="s" access="read"/>
            <property name="ServiceUUIDs" type="as" access="read"/>
            <property name="ManufacturerData" type="a{qv}" access="read"/>
            <property name="ServiceData" type="a{sv}" access="read"/>
            <property name="Includes" type="as" access="read"/>
            <property name="LocalName" type="s" access="read"/>
            <property name="Discoverable" type="b" access="read"/>
          </interface>
          <interface name="org.freedesktop.DBus.Properties">
            <method name="Get">
              <arg name="interface" type="s" direction="in"/>
              <arg name="name" type="s" direction="in"/>
              <arg name="value" type="v" direction="out"/>
            </method>
            <method name="GetAll">
              <arg name="interface" type="s" direction="in"/>
              <arg name="properties" type="a{sv}" direction="out"/>
            </method>
          </interface>
          <interface name="org.freedesktop.DBus.Introspectable">
            <method name="Introspect">
              <arg name="xml" type="s" direction="out"/>
            </method>
          </interface>
        </node>
        """;
}


/// <summary>
/// The payload an exported <see cref="LEAdvertisement"/> reports to BlueZ.
/// </summary>
internal sealed class LEAdvertisementProperties
{
    /// <summary>
    /// <c>peripheral</c> for a connectable advertisement, <c>broadcast</c> for a one-way one.
    /// Beacons are broadcast - they have nothing to connect to.
    /// </summary>
    public string Type { get; init; } = "peripheral";

    /// <summary>Whether <see cref="Type"/> is the connectable one.</summary>
    public bool IsConnectable => this.Type == "peripheral";

    public string? LocalName { get; init; }
    public bool Discoverable { get; init; } = true;
    public IReadOnlyList<string> ServiceUuids { get; init; } = [];
    public IReadOnlyList<string> Includes { get; init; } = [];
    public IReadOnlyDictionary<string, byte[]> ServiceData { get; init; } = new Dictionary<string, byte[]>();
    public IReadOnlyDictionary<ushort, byte[]> ManufacturerData { get; init; } = new Dictionary<ushort, byte[]>();


    /// <summary>
    /// The property names that actually carry a value.
    /// </summary>
    /// <remarks>
    /// BlueZ rejects the whole advertisement if a property is present but empty - an empty
    /// <c>ServiceUUIDs</c> array or a zero-length <c>LocalName</c> both fail registration - so
    /// anything unset is omitted from GetAll rather than sent blank.
    /// </remarks>
    public IEnumerable<string> PresentPropertyNames()
    {
        yield return "Type";

        // BlueZ refuses an advertisement that sets Discoverable alongside Type=broadcast - the
        // spec says the property "shall not be set" there - so it is only reported for a
        // connectable one.
        if (this.IsConnectable)
            yield return "Discoverable";

        if (this.LocalName != null)
            yield return "LocalName";

        if (this.ServiceUuids.Count > 0)
            yield return "ServiceUUIDs";

        if (this.Includes.Count > 0)
            yield return "Includes";

        if (this.ServiceData.Count > 0)
            yield return "ServiceData";

        if (this.ManufacturerData.Count > 0)
            yield return "ManufacturerData";
    }
}
