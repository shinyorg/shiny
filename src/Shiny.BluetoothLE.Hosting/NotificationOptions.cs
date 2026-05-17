using System;

namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// Notification modes supported by a hosted GATT characteristic.
/// </summary>
[Flags]
public enum NotificationOptions
{
    /// <summary>Sends unacknowledged notifications.</summary>
    Notify,

    /// <summary>Sends acknowledged indications.</summary>
    Indicate,

    /// <summary>Requires the link to be encrypted before sending notifications/indications.</summary>
    EncryptionRequired
}
