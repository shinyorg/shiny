using System.Threading;
using System.Threading.Tasks;
using Tmds.DBus.Protocol;

namespace Shiny.BluetoothLE.Bluez;


internal class BluezGattService
{
    readonly Connection connection;
    readonly string objectPath;

    public BluezGattService(Connection connection, string objectPath)
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
            BluezConstants.GattServiceInterface,
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


    public Task<bool> GetPrimaryAsync(CancellationToken ct = default)
    {
        var msg = this.connection.CreateGetPropertyCall(
            BluezConstants.Service,
            this.objectPath,
            BluezConstants.GattServiceInterface,
            "Primary"
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
}
