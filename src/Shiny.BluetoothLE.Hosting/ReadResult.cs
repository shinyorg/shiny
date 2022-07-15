using System;

namespace Shiny.BluetoothLE.Hosting;


public record ReadResult(
    GattState Status,
    byte[]? Data
)
{
    public static ReadResult Success(byte[] data)
        => new ReadResult(GattState.Success, data);

    public static ReadResult Error(GattState status) => new ReadResult(status, null);
}
