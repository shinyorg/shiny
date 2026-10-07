using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Shiny.BluetoothLE.Bluez;


internal class BluezGattCharacteristic
{
    readonly DBusConnection connection;
    readonly string objectPath;

    public BluezGattCharacteristic(DBusConnection connection, string objectPath)
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
            BluezConstants.GattCharacteristicInterface,
            "UUID"
        );
        return this.connection.CallAsync(
            msg,
            static (Message reply, object? _) =>
            {
                var reader = reply.GetBodyReader();
                return reader.ReadStringVariant()!;
            },
            ct
        );
    }


    public Task<string[]> GetFlagsAsync(CancellationToken ct = default)
    {
        var msg = this.connection.CreateGetPropertyCall(
            BluezConstants.Service,
            this.objectPath,
            BluezConstants.GattCharacteristicInterface,
            "Flags"
        );
        return this.connection.CallAsync(
            msg,
            static (Message reply, object? _) =>
            {
                var reader = reply.GetBodyReader();
                return reader.ReadStringArrayVariant();
            },
            ct
        );
    }


    public Task<byte[]> ReadValueAsync(CancellationToken ct = default)
    {
        var writer = this.connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: BluezConstants.Service,
            path: this.objectPath,
            @interface: BluezConstants.GattCharacteristicInterface,
            member: "ReadValue",
            signature: "a{sv}"
        );

        // empty options dict
        writer.WriteDictionary(new Dictionary<string, VariantValue>());

        var msg = writer.CreateMessage();
        return this.connection.CallAsync(
            msg,
            static (Message reply, object? _) =>
            {
                var reader = reply.GetBodyReader();
                return reader.ReadArrayOfByte();
            },
            ct
        );
    }


    public async Task WriteValueAsync(byte[] value, bool withResponse = true, CancellationToken ct = default)
    {
        var writer = this.connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: BluezConstants.Service,
            path: this.objectPath,
            @interface: BluezConstants.GattCharacteristicInterface,
            member: "WriteValue",
            signature: "aya{sv}"
        );

        writer.WriteArray(value);
        writer.WriteDictionary(new Dictionary<string, VariantValue>
        {
            ["type"] = VariantValue.String(withResponse ? "request" : "command")
        });

        var msg = writer.CreateMessage();
        await this.connection.CallAsync(msg, ct).ConfigureAwait(false);
    }


    public async Task StartNotifyAsync(CancellationToken ct = default)
    {
        var msg = this.connection.CreateMethodCall(
            BluezConstants.Service,
            this.objectPath,
            BluezConstants.GattCharacteristicInterface,
            "StartNotify"
        );
        await this.connection.CallAsync(msg, ct).ConfigureAwait(false);
    }


    public async Task StopNotifyAsync(CancellationToken ct = default)
    {
        var msg = this.connection.CreateMethodCall(
            BluezConstants.Service,
            this.objectPath,
            BluezConstants.GattCharacteristicInterface,
            "StopNotify"
        );
        await this.connection.CallAsync(msg, ct).ConfigureAwait(false);
    }


    public static CharacteristicProperties FlagsToProperties(string[] flags)
    {
        var props = (CharacteristicProperties)0;
        foreach (var flag in flags)
        {
            switch (flag)
            {
                case "read":
                    props |= CharacteristicProperties.Read;
                    break;
                case "write":
                    props |= CharacteristicProperties.Write;
                    break;
                case "write-without-response":
                    props |= CharacteristicProperties.WriteWithoutResponse;
                    break;
                case "notify":
                    props |= CharacteristicProperties.Notify;
                    break;
                case "indicate":
                    props |= CharacteristicProperties.Indicate;
                    break;
                case "broadcast":
                    props |= CharacteristicProperties.Broadcast;
                    break;
                case "authenticated-signed-writes":
                    props |= CharacteristicProperties.AuthenticatedSignedWrites;
                    break;
                case "extended-properties":
                    props |= CharacteristicProperties.ExtendedProperties;
                    break;
            }
        }
        return props;
    }
}
