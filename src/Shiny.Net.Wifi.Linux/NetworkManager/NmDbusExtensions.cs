using Tmds.DBus.Protocol;

namespace Shiny.Net.Wifi.NetworkManager;


/// <summary>
/// The small slice of raw D-Bus plumbing NetworkManager needs. Tmds.DBus.Protocol is deliberately
/// codegen-free, so messages are written and read by hand.
/// </summary>
internal static class NmDbusExtensions
{
    /// <summary>
    /// The human-readable half of a D-Bus failure. An error reply carries the daemon's own message
    /// (the polkit refusal, the driver complaint); a transport failure only has the exception text.
    /// </summary>
    public static string Describe(this DBusExceptionBase exception)
        => (exception as DBusErrorReplyException)?.ErrorMessage ?? exception.Message;


    public static MessageBuffer CreateMethodCall(
        this DBusConnection connection,
        string path,
        string @interface,
        string method,
        string? signature = null
    )
    {
        var writer = connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: NmConstants.Service,
            path: path,
            @interface: @interface,
            member: method,
            signature: signature
        );
        return writer.CreateMessage();
    }


    public static MessageBuffer CreateGetPropertyCall(
        this DBusConnection connection,
        string path,
        string @interface,
        string property
    )
    {
        var writer = connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: NmConstants.Service,
            path: path,
            @interface: NmConstants.PropertiesInterface,
            member: "Get",
            signature: "ss"
        );
        writer.WriteString(@interface);
        writer.WriteString(property);
        return writer.CreateMessage();
    }


    public static Task<T> GetProperty<T>(
        this DBusConnection connection,
        string path,
        string @interface,
        string property,
        MessageValueReader<T> read
    )
    {
        var msg = connection.CreateGetPropertyCall(path, @interface, property);
        return connection.CallMethodAsync(msg, read);
    }


    public static Task<bool> GetBoolProperty(this DBusConnection connection, string path, string @interface, string property)
        => connection.GetProperty(path, @interface, property, static (Message reply, object? _) =>
        {
            var reader = reply.GetBodyReader();
            reader.ReadSignature();
            return reader.ReadBool();
        });


    public static Task<uint> GetUInt32Property(this DBusConnection connection, string path, string @interface, string property)
        => connection.GetProperty(path, @interface, property, static (Message reply, object? _) =>
        {
            var reader = reply.GetBodyReader();
            reader.ReadSignature();
            return reader.ReadUInt32();
        });


    public static Task<byte> GetByteProperty(this DBusConnection connection, string path, string @interface, string property)
        => connection.GetProperty(path, @interface, property, static (Message reply, object? _) =>
        {
            var reader = reply.GetBodyReader();
            reader.ReadSignature();
            return reader.ReadByte();
        });


    public static Task<string> GetStringProperty(this DBusConnection connection, string path, string @interface, string property)
        => connection.GetProperty(path, @interface, property, static (Message reply, object? _) =>
        {
            var reader = reply.GetBodyReader();
            reader.ReadSignature();
            return reader.ReadString();
        });


    public static Task<string> GetObjectPathProperty(this DBusConnection connection, string path, string @interface, string property)
        => connection.GetProperty(path, @interface, property, static (Message reply, object? _) =>
        {
            var reader = reply.GetBodyReader();
            reader.ReadSignature();
            return reader.ReadObjectPathAsString();
        });


    public static Task<string[]> GetObjectPathArrayProperty(this DBusConnection connection, string path, string @interface, string property)
        => connection.GetProperty(path, @interface, property, static (Message reply, object? _) =>
        {
            var reader = reply.GetBodyReader();
            reader.ReadSignature();
            return reader.ReadArrayOfObjectPath().Select(x => x.ToString()).ToArray();
        });


    public static Task<byte[]> GetByteArrayProperty(this DBusConnection connection, string path, string @interface, string property)
        => connection.GetProperty(path, @interface, property, static (Message reply, object? _) =>
        {
            var reader = reply.GetBodyReader();
            reader.ReadSignature();
            return reader.ReadArrayOfByte();
        });


    /// <summary>
    /// Reads an <c>aa{sv}</c> property - the shape NetworkManager uses for AddressData and
    /// NameserverData, where each entry is a small dictionary rather than a struct.
    /// </summary>
    public static Task<List<Dictionary<string, VariantValue>>> GetDictArrayProperty(
        this DBusConnection connection,
        string path,
        string @interface,
        string property
    )
        => connection.GetProperty(path, @interface, property, static (Message reply, object? _) =>
        {
            var reader = reply.GetBodyReader();
            reader.ReadSignature();

            var results = new List<Dictionary<string, VariantValue>>();
            var arrayEnd = reader.ReadArrayStart(DBusType.Array);
            while (reader.HasNext(arrayEnd))
                results.Add(reader.ReadDictionaryOfStringToVariantValue());

            return results;
        });


    public static async Task SetBoolProperty(
        this DBusConnection connection,
        string path,
        string @interface,
        string property,
        bool value
    )
    {
        var writer = connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: NmConstants.Service,
            path: path,
            @interface: NmConstants.PropertiesInterface,
            member: "Set",
            signature: "ssv"
        );
        writer.WriteString(@interface);
        writer.WriteString(property);
        writer.WriteVariantBool(value);

        await connection.CallMethodAsync(writer.CreateMessage()).ConfigureAwait(false);
    }


    /// <summary>
    /// Writes an <c>a{sa{sv}}</c> connection settings body - a dictionary of setting groups, each
    /// itself a dictionary of variants.
    /// </summary>
    /// <remarks>
    /// There is no VariantValue factory for a nested dictionary, so the two levels are written with
    /// the low-level dictionary primitives rather than <c>WriteDictionary</c>.
    /// </remarks>
    public static void WriteConnectionSettings(this MessageWriter writer, NmConnectionSettings settings)
    {
        var outer = writer.WriteDictionaryStart();
        foreach (var group in settings.Groups)
        {
            writer.WriteDictionaryEntryStart();
            writer.WriteString(group.Key);

            var inner = writer.WriteDictionaryStart();
            foreach (var entry in group.Value)
            {
                writer.WriteDictionaryEntryStart();
                writer.WriteString(entry.Key);
                writer.WriteVariant(entry.Value);
            }
            writer.WriteDictionaryEnd(inner);
        }
        writer.WriteDictionaryEnd(outer);
    }
}


/// <summary>
/// A NetworkManager connection profile - the <c>a{sa{sv}}</c> shape, kept as an ordered set of
/// named setting groups ("connection", "802-11-wireless", "ipv4", ...).
/// </summary>
internal sealed class NmConnectionSettings
{
    public Dictionary<string, Dictionary<string, VariantValue>> Groups { get; } = new();

    public Dictionary<string, VariantValue> Group(string name)
    {
        if (!this.Groups.TryGetValue(name, out var group))
        {
            group = new Dictionary<string, VariantValue>();
            this.Groups[name] = group;
        }
        return group;
    }
}
