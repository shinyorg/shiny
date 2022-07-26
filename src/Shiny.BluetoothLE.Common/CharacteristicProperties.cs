using System;

namespace Shiny.BluetoothLE;


[Flags]
public enum CharacteristicProperties
{
    Broadcast = 1, //0x1
    Read = 2, //0x2
    WriteWithoutResponse = 4, //0x4
    Write = 8, //0x8
    Notify = 16, //0x10
    Indicate = 32, //0x20
    AuthenticatedSignedWrites = 64, //0x40
    ExtendedProperties = 128, //0x80
    NotifyEncryptionRequired = 256, //0x100
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