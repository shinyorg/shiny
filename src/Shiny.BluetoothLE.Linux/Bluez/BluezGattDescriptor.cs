using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Shiny.BluetoothLE.Bluez;


internal class BluezGattDescriptor
{
    readonly DBusConnection connection;
    readonly string objectPath;

    public BluezGattDescriptor(DBusConnection connection, string objectPath)
    {
        this.connection = connection;
        this.objectPath = objectPath;
    }


    public string ObjectPath => this.objectPath;


    public Task<string> GetUuidAsync(CancellationToken ct = default)
    {
        var msg = this.connection.CreateGetPropertyCall(
            BluezConstants.Service,
            this.objectPath,
            BluezConstants.GattDescriptorInterface,
            "UUID"
        );
        return this.connection.CallMethodAsync(
            msg,
            static (Message reply, object? _) =>
            {
                var reader = reply.GetBodyReader();
                return reader.ReadStringVariant()!;
            }
        );
    }


    public Task<byte[]> ReadValueAsync(Dictionary<string, object>? options = null, CancellationToken ct = default)
    {
        var writer = this.connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: BluezConstants.Service,
            path: this.objectPath,
            @interface: BluezConstants.GattDescriptorInterface,
            member: "ReadValue",
            signature: "a{sv}"
        );

        writer.WriteDictionary(new Dictionary<string, VariantValue>());

        var msg = writer.CreateMessage();
        return this.connection.CallMethodAsync(
            msg,
            static (Message reply, object? _) =>
            {
                var reader = reply.GetBodyReader();
                return reader.ReadArrayOfByte();
            }
        );
    }


    public async Task WriteValueAsync(byte[] value, CancellationToken ct = default)
    {
        var writer = this.connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: BluezConstants.Service,
            path: this.objectPath,
            @interface: BluezConstants.GattDescriptorInterface,
            member: "WriteValue",
            signature: "aya{sv}"
        );

        writer.WriteArray(value);
        writer.WriteDictionary(new Dictionary<string, VariantValue>());

        var msg = writer.CreateMessage();
        await this.connection.CallMethodAsync(msg).ConfigureAwait(false);
    }
}
