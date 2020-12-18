using System;


namespace Shiny.BluetoothLE
{
    // https://www.bluetooth.org/en-us/specification/assigned-numbers/generic-access-profile
    public enum AdvertisementRecordType
    {
        Flags = 0x01,
        IncompleteUuids16Bit = 0x02,
        CompleteUuids16Bit = 0x03,

        IncompleteUuids32Bit = 0x04,
        CompleteUuids32Bit = 0x05,

        IncompleteUuids128Bit = 0x06,
        CompleteUuid128Bit = 0x07,

        ShortLocalName = 0x08,
        CompleteLocalName = 0x09,

        TxPowerLevel = 0x0A,
        DeviceClass = 0x0D,

        SimplePairingHash = 0x0E,
        SimplePairingRandomizer = 0x0F,

        DeviceId = 0x10,
        //SecurityManagerTkValue = 0x10, // ???
        SecurityManagerOutOfBandFlags = 0x11,
        SlaveConnectionIntervalRange = 0x12,

        ServiceUuids16Bit = 0x14,
        ServiceUuids32Bit = 0x1F,
        ServiceUuids128Bit = 0x15,

        ServiceData16Bit = 0x16,
        ServiceData32Bit = 0x20,
        ServiceData128Bit = 0x21,

        LeSecureConnectionsConfirmationValue = 0x22,
        LeSecureConnectionsRandomValue = 0x23,
        Uri = 0x24,
        IndoorPositioning = 0x25,
        TransportDiscoveryData = 0x26,

        PublicTargetAddress = 0x17,
        RandomTargetAddress = 0x18,
        Appearance = 0x19,

        AdvertisingInternval = 0x1A,
        LeDeviceAddress = 0x1B,
        LeRole = 0x1C,

        SimplePairingHashC256 = 0x1D,
        SimplePairingRandomizerR256 = 0x1E,
        Information3DData = 0x3D,
        ManufacturerSpecificData = 0xFF
    }
}
