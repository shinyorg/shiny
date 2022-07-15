using System;

namespace Shiny.BluetoothLE.Hosting;


[Flags]
public enum WriteOptions
{
    Write,
    WriteWithoutResponse,
    AuthenticatedSignedWrites,
    EncryptionRequired
}
