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
        await this.connection.CallAsync(msg, ct).ConfigureAwait(false);
    }


    public async Task DisconnectAsync(CancellationToken ct = default)
    {
        var msg = this.connection.CreateMethodCall(
            BluezConstants.Service,
            this.objectPath,
            BluezConstants.DeviceInterface,
            "Disconnect"
        );
        await this.connection.CallAsync(msg, ct).ConfigureAwait(false);
    }


    public Task<string> GetAddressAsync(CancellationToken ct = default)
    {
        var msg = this.connection.CreateGetPropertyCall(
            BluezConstants.Service,
            this.objectPath,
            BluezConstants.DeviceInterface,
            "Address"
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
            return await this.connection.CallAsync(
                msg,
                static (Message reply, object? _) =>
                {
                    var reader = reply.GetBodyReader();
                    return reader.ReadStringVariant()!;
                },
                ct
            ).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return "public";
        }
    }


    public Task<bool> GetServicesResolvedAsync(CancellationToken ct = default)
    {
        var msg = this.connection.CreateGetPropertyCall(
            BluezConstants.Service,
            this.objectPath,
            BluezConstants.DeviceInterface,
            "ServicesResolved"
        );
        return this.connection.CallAsync(
            msg,
            static (Message reply, object? _) =>
            {
                var reader = reply.GetBodyReader();
                return reader.ReadBoolVariant();
            },
            ct
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
            return await this.connection.CallAsync(
                msg,
                static (Message reply, object? _) =>
                {
                    var reader = reply.GetBodyReader();
                    return reader.ReadInt16Variant();
                },
                ct
            ).ConfigureAwait(false);
        }
        catch (Exception ex) when (ex is not OperationCanceledException)
        {
            return 0;
        }
    }
}
