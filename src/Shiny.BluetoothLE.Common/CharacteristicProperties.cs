using System;

namespace Shiny.BluetoothLE;


/// <summary>
/// GATT characteristic property flags declared by a remote characteristic.
/// </summary>
[Flags]
public enum CharacteristicProperties
{
    /// <summary>The characteristic value may be broadcast in advertisements.</summary>
    Broadcast = 1, //0x1

    /// <summary>The characteristic value may be read.</summary>
    Read = 2, //0x2

    /// <summary>The characteristic supports writes without a response.</summary>
    WriteWithoutResponse = 4, //0x4

    /// <summary>The characteristic supports writes with a response.</summary>
    Write = 8, //0x8

    /// <summary>The characteristic may send unacknowledged notifications.</summary>
    Notify = 16, //0x10

    /// <summary>The characteristic may send acknowledged indications.</summary>
    Indicate = 32, //0x20

    /// <summary>The characteristic supports authenticated signed writes.</summary>
    AuthenticatedSignedWrites = 64, //0x40

    /// <summary>Additional extended properties are described by a descriptor.</summary>
    ExtendedProperties = 128, //0x80

    /// <summary>Notifications require an encrypted link.</summary>
    NotifyEncryptionRequired = 256, //0x100

    /// <summary>Indications require an encrypted link.</summary>
    IndicateEncryptionRequired = 512, //0x200

    //iOS
    //		Broadcast = 0x01,
    //		Read = 0x02,
    //		WriteWithoutResponse = 0x04,
    //		PropertyWrite = 0x08,
    //		Notify = 0x10,
    //		Indicate = 0x20,
    //		AuthenticatedSignedWrites = 0x40,
    //		ExtendedProperties = 0x80,
    //		NotifyEncryptionRequired = 0x100,
    //		IndicateEncryptionRequired = 0x200,

    // Droid
    //		Broadcast = 1, //0x1
    //		Read = 2, //0x2
    //		Write = 8, //0x8
    //		Notify = 16, //0x10
    //		Indicate = 32, //0x20
    //		SignedWrite = 64, //0x40
    //		ExtendedProperties = 128, //0x80
}
