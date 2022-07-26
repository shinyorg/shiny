using System;

namespace Shiny.BluetoothLE.Hosting;


[Flags]
public enum NotificationOptions
{
    Notify,
    Indicate,
    EncryptionRequired
}
