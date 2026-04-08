using System;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Shiny.BluetoothLE.Bluez;


internal class BluezAdapter
{
    readonly Connection connection;
    readonly string objectPath;

    public BluezAdapter(Connection connection, string objectPath = BluezConstants.DefaultAdapterPath)
    {
        this.connection = connection;
        this.objectPath = objectPath;
    }


    public string ObjectPath => this.objectPath;


    public async Task StartDiscoveryAsync(CancellationToken ct = default)
    {
        var msg = this.connection.CreateMethodCall(
            BluezConstants.Service,
            this.objectPath,
            BluezConstants.AdapterInterface,
            "StartDiscovery"
        );
        await this.connection.CallMethodAsync(msg).ConfigureAwait(false);
    }


    public async Task StopDiscoveryAsync(CancellationToken ct = default)
    {
        var msg = this.connection.CreateMethodCall(
            BluezConstants.Service,
            this.objectPath,
            BluezConstants.AdapterInterface,
            "StopDiscovery"
        );
        await this.connection.CallMethodAsync(msg).ConfigureAwait(false);
    }


    public async Task SetDiscoveryFilterAsync(string transport = "le", CancellationToken ct = default)
    {
        var writer = this.connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: BluezConstants.Service,
            path: this.objectPath,
            @interface: BluezConstants.AdapterInterface,
            member: "SetDiscoveryFilter",
            signature: "a{sv}"
        );

        writer.WriteDictionary(new Dictionary<string, VariantValue>
        {
            ["Transport"] = VariantValue.String(transport)
        });

        var msg = writer.CreateMessage();
        await this.connection.CallMethodAsync(msg).ConfigureAwait(false);
    }


    public Task<bool> GetPoweredAsync(CancellationToken ct = default)
    {
        var msg = this.connection.CreateGetPropertyCall(
            BluezConstants.Service,
            this.objectPath,
            BluezConstants.AdapterInterface,
            "Powered"
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


    public Task<bool> GetDiscoveringAsync(CancellationToken ct = default)
    {
        var msg = this.connection.CreateGetPropertyCall(
            BluezConstants.Service,
            this.objectPath,
            BluezConstants.AdapterInterface,
            "Discovering"
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


    public async Task RemoveDeviceAsync(string devicePath, CancellationToken ct = default)
    {
        var writer = this.connection.GetMessageWriter();
        writer.WriteMethodCallHeader(
            destination: BluezConstants.Service,
            path: this.objectPath,
            @interface: BluezConstants.AdapterInterface,
            member: "RemoveDevice",
            signature: "o"
        );
        writer.WriteObjectPath(devicePath);

        var msg = writer.CreateMessage();
        await this.connection.CallMethodAsync(msg).ConfigureAwait(false);
    }
}
