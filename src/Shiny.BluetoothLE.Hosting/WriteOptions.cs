using System;


namespace Shiny.BluetoothLE.Peripherals
{
    [Flags]
    public enum WriteOptions
    {
        Write,
        WriteWithoutResponse,
        AuthenticatedSignedWrites,
        EncryptionRequired
    }
}
