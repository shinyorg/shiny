using System;


namespace Shiny.BluetoothLE.Hosting
{
    /// <summary>
    /// GATT response status codes returned to a central from a hosted read or write operation.
    /// </summary>
    public enum GattState
    {
        /// <summary>The operation completed successfully.</summary>
        Success = 0,
        // iOS - InvalidHandle = 1

        /// <summary>The characteristic does not permit reads.</summary>
        ReadNotPermitted = 2,

        /// <summary>The characteristic does not permit writes.</summary>
        WriteNotPermitted = 3,
        // iOS - InvalidPdu

        /// <summary>The operation requires authentication and the link is not authenticated.</summary>
        InsufficientAuthentication = 5,

        /// <summary>The requested operation is not supported on this attribute.</summary>
        RequestNotSupported = 6,

        /// <summary>The offset in a partial read/write is outside the attribute's value range.</summary>
        InvalidOffset = 7,

        //InsufficientAuthorization,
        //  PrepareQueueFull,
        //  AttributeNotFound,
        //  AttributeNotLong,
        //  InsufficientEncryptionKeySize,

        /// <summary>The supplied attribute value has an invalid length.</summary>
        InvalidAttributeLength = 13,

        //  InvalidAttributeValueLength,
        //UnlikelyError

        /// <summary>The operation requires an encrypted link.</summary>
        InsufficientEncryption = 15,

        //UnsupportedGroupType

        /// <summary>The stack ran out of resources to complete the operation (Android maps to ConnectionCongested).</summary>
        InsufficientResources = 143, // droid ConnectionCongested

        /// <summary>A generic failure occurred.</summary>
        Failure = 257
    }
}
