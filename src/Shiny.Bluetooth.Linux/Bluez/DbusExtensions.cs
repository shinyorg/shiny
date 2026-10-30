using Tmds.DBus.Protocol;

namespace Shiny.Bluetooth.Bluez;


internal static class DbusExtensions
{
    // Tmds.DBus.Protocol's CallMethodAsync takes no CancellationToken, so cancelling stops the caller
    // waiting and the reply is dropped - BlueZ still runs the call once it is sent. Named CallAsync
    // rather than overloading CallMethodAsync, whose generic form has an optional object readerState
    // that would quietly bind a CancellationToken.
    public static Task CallAsync(this DBusConnection connection, MessageBuffer message, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return connection.CallMethodAsync(message).WaitAsync(ct);
    }


    public static Task<T> CallAsync<T>(this DBusConnection connection, MessageBuffer message, MessageValueReader<T> reader, CancellationToken ct)
    {
        ct.ThrowIfCancellationRequested();
        return connection.CallMethodAsync(message, reader).WaitAsync(ct);
    }


    public static MessageBuffer CreateMethodCall(this DBusConnection connection, string path, string @interface, string method)
    {
        var writer = connection.GetMessageWriter();
        try
        {
            writer.WriteMethodCallHeader(destination: BluezConstants.Service, path: path, @interface: @interface, member: method);
            return writer.CreateMessage();
        }
        finally
        {
            writer.Dispose();
        }
    }


    public static MessageBuffer CreateMethodCall(this DBusConnection connection, string path, string @interface, string method, string argument)
    {
        var writer = connection.GetMessageWriter();
        try
        {
            writer.WriteMethodCallHeader(destination: BluezConstants.Service, path: path, @interface: @interface, member: method, signature: "s");
            writer.WriteString(argument);
            return writer.CreateMessage();
        }
        finally
        {
            writer.Dispose();
        }
    }


    public static MessageBuffer CreateSetPropertyCall(this DBusConnection connection, string path, string @interface, string property, VariantValue value)
    {
        var writer = connection.GetMessageWriter();
        try
        {
            writer.WriteMethodCallHeader(
                destination: BluezConstants.Service,
                path: path,
                @interface: BluezConstants.PropertiesInterface,
                member: "Set",
                signature: "ssv"
            );
            writer.WriteString(@interface);
            writer.WriteString(property);
            writer.WriteVariant(value);
            return writer.CreateMessage();
        }
        finally
        {
            writer.Dispose();
        }
    }


    /// <summary>
    /// Reads a <c>GetManagedObjects</c> reply (<c>a{oa{sa{sv}}}</c>) down to the <c>Device1</c> property
    /// sets under <paramref name="adapterPath"/>.
    /// </summary>
    /// <remarks>
    /// <see cref="Reader"/> is a ref struct, so it is only ever advanced in place here - the property
    /// dictionaries go through <see cref="Reader.ReadDictionaryOfStringToVariantValue"/>, which reads
    /// each variant's own signature, rather than hand-rolled variant helpers.
    /// </remarks>
    public static List<(string Path, Dictionary<string, VariantValue> Properties)> ReadDevices(Message reply, string adapterPath)
    {
        var result = new List<(string, Dictionary<string, VariantValue>)>();
        var prefix = adapterPath + "/";
        var reader = reply.GetBodyReader();

        var objectsEnd = reader.ReadArrayStart(DBusType.DictEntry);
        while (reader.HasNext(objectsEnd))
        {
            var path = reader.ReadObjectPathAsString();
            var interfacesEnd = reader.ReadArrayStart(DBusType.DictEntry);
            while (reader.HasNext(interfacesEnd))
            {
                var iface = reader.ReadString();
                var properties = reader.ReadDictionaryOfStringToVariantValue();
                if (iface == BluezConstants.DeviceInterface && path.StartsWith(prefix, StringComparison.Ordinal))
                    result.Add((path, properties));
            }
        }
        return result;
    }
}
