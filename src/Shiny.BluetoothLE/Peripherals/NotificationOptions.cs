using System;


namespace Shiny.BluetoothLE.Peripherals
{
    [Flags]
    public enum NotificationOptions
    {
        Notify,
        Indicate,
        EncryptionRequired
    }
}
