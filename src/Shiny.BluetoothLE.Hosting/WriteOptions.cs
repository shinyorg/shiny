using System;

namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// Write modes supported by a hosted GATT characteristic.
/// </summary>
[Flags]
public enum WriteOptions
{
    /// <summary>Accepts standard writes with response.</summary>
    Write,

    /// <summary>Accepts unacknowledged writes without response.</summary>
    WriteWithoutResponse,

    /// <summary>Accepts authenticated signed writes.</summary>
    AuthenticatedSignedWrites,

    /// <summary>Requires the link to be encrypted before accepting writes.</summary>
    EncryptionRequired
}
