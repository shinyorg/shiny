using System;


namespace Shiny.BluetoothLE.Hosting
{
    public enum GattState
    {
        Success = 0,
        // iOS - InvalidHandle = 1
        ReadNotPermitted = 2,
        WriteNotPermitted = 3,
        // iOS - InvalidPdu
        InsufficientAuthentication = 5,
        RequestNotSupported = 6,
        InvalidOffset = 7,
        //InsufficientAuthorization,
        //  PrepareQueueFull,
        //  AttributeNotFound,
        //  AttributeNotLong,
        //  InsufficientEncryptionKeySize,
        InvalidAttributeLength = 13,
        //  InvalidAttributeValueLength,
        //UnlikelyError
        InsufficientEncryption = 15,
        //UnsupportedGroupType
        InsufficientResources = 143, // droid ConnectionCongested
        Failure = 257
    }
}
