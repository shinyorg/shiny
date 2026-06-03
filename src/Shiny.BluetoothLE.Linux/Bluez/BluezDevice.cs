using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Shiny.BluetoothLE.Bluez;


internal class BluezDevice
{
    readonly DBusConnection connection;
    readonly string objectPath;

    public BluezDevice(DBusConnection connection, string objectPath)
    {
        this.connection = connection;
        this.objectPath = objectPath;
    }


    public string ObjectPath => this.objectPath;


    public async Task ConnectAsync(CancellationToken ct = default)
    {
        var msg = this.connection.CreateMethodCall(
            BluezConstants.Service,
            this.objectPath,
            BluezConstants.DeviceInterface,
            "Connect"
        );
        await this.connection.CallMethodAsync(msg).ConfigureAwait(false);
    }


    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        var msg = this.connection.CreateMethodCall(
            BluezConstants.Service,
            this.objectPath,
            BluezConstants.DeviceInterface,
            "Disconnect"
        );
        await this.connection.CallMethodAsync(msg).ConfigureAwait(false);
    }


    public async Task<string?> GetNameAsync(CancellationToken ct = default)
    {
        try
        {
            var msg = this.connection.CreateGetPropertyCall(
                BluezConstants.Service,
                this.objectPath,
                BluezConstants.DeviceInterface,
                "Name"
            );
            return await this.connection.CallMethodAsync(
                msg,
                static (Message reply, object? _) =>
                {
                    var reader = reply.GetBodyReader();
                    return reader.ReadStringVariant();
                }
            ).ConfigureAwait(false);
        }
        catch
        {
            return null;
        }
    }


    public Task<string> GetAddressAsync(CancellationToken ct = default)
    {
        var msg = this.connection.CreateGetPropertyCall(
            BluezConstants.Service,
            this.objectPath,
            BluezConstants.DeviceInterface,
            "Address"
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


    public async Task<string> GetAddressTypeAsync(CancellationToken ct = default)
    {
        // BlueZ Device1.AddressType is "public" or "random". Older BlueZ may omit the property
        // for classic-only devices; default to "public" in that case.
        try
        {
            var msg = this.connection.CreateGetPropertyCall(
                BluezConstants.Service,
                this.objectPath,
                BluezConstants.DeviceInterface,
                "AddressType"
            );
            return await this.connection.CallMethodAsync(
                msg,
                static (Message reply, object? _) =>
                {
                    var reader = reply.GetBodyReader();
                    return reader.ReadStringVariant()!;
                }
            ).ConfigureAwait(false);
        }
        catch
        {
            return "public";
        }
    }


    public Task<bool> GetConnectedAsync(CancellationToken ct = default)
    {
        var msg = this.connection.CreateGetPropertyCall(
            BluezConstants.Service,
            this.objectPath,
            BluezConstants.DeviceInterface,
            "Connected"
        );
        return this.connection.CallMethodAsync(
            msg,
            static (Message reply, object? _) =>
            {
                var reader = reply.GetBodyReader();
                return reader.ReadBoolVariant();
            }
        );
    }


    public Task<bool> GetServicesResolvedAsync(CancellationToken ct = default)
    {
        var msg = this.connection.CreateGetPropertyCall(
            BluezConstants.Service,
            this.objectPath,
            BluezConstants.DeviceInterface,
            "ServicesResolved"
        );
        return this.connection.CallMethodAsync(
            msg,
            static (Message reply, object? _) =>
            {
                var reader = reply.GetBodyReader();
                return reader.ReadBoolVariant();
            }
        );
    }


    public async Task<short> GetRssiAsync(CancellationToken ct = default)
    {
        try
        {
            var msg = this.connection.CreateGetPropertyCall(
                BluezConstants.Service,
                this.objectPath,
                BluezConstants.DeviceInterface,
                "RSSI"
            );
            return await this.connection.CallMethodAsync(
                msg,
                static (Message reply, object? _) =>
                {
                    var reader = reply.GetBodyReader();
                    return reader.ReadInt16Variant();
                }
            ).ConfigureAwait(false);
        }
        catch
        {
            return 0;
        }
    }


    public async Task<string[]> GetUuidsAsync(CancellationToken ct = default)
    {
        try
        {
            var msg = this.connection.CreateGetPropertyCall(
                BluezConstants.Service,
                this.objectPath,
                BluezConstants.DeviceInterface,
                "UUIDs"
            );
            return await this.connection.CallMethodAsync(
                msg,
                static (Message reply, object? _) =>
                {
                    var reader = reply.GetBodyReader();
                    return reader.ReadStringArrayVariant();
                }
            ).ConfigureAwait(false);
        }
        catch
        {
            return Array.Empty<string>();
        }
    }


    public async Task<short> GetTxPowerAsync(CancellationToken ct = default)
    {
        try
        {
            var msg = this.connection.CreateGetPropertyCall(
                BluezConstants.Service,
                this.objectPath,
                BluezConstants.DeviceInterface,
                "TxPower"
            );
            return await this.connection.CallMethodAsync(
                msg,
                static (Message reply, object? _) =>
                {
                    var reader = reply.GetBodyReader();
                    return reader.ReadInt16Variant();
                }
            ).ConfigureAwait(false);
        }
        catch
        {
            return 0;
        }
    }


    public async Task<Dictionary<ushort, byte[]>> GetManufacturerDataAsync(CancellationToken ct = default)
    {
        try
        {
            var msg = this.connection.CreateGetPropertyCall(
                BluezConstants.Service,
                this.objectPath,
                BluezConstants.DeviceInterface,
                "ManufacturerData"
            );
            return await this.connection.CallMethodAsync(
                msg,
                static (Message reply, object? _) =>
                {
                    var reader = reply.GetBodyReader();
                    return reader.ReadManufacturerDataVariant();
                }
            ).ConfigureAwait(false);
        }
        catch
        {
            return new Dictionary<ushort, byte[]>();
        }
    }


    public async Task<Dictionary<string, byte[]>> GetServiceDataAsync(CancellationToken ct = default)
    {
        try
        {
            var msg = this.connection.CreateGetPropertyCall(
                BluezConstants.Service,
                this.objectPath,
                BluezConstants.DeviceInterface,
                "ServiceData"
            );
            return await this.connection.CallMethodAsync(
                msg,
                static (Message reply, object? _) =>
                {
                    var reader = reply.GetBodyReader();
                    return reader.ReadServiceDataVariant();
                }
            ).ConfigureAwait(false);
        }
        catch
        {
            return new Dictionary<string, byte[]>();
        }
    }
}
