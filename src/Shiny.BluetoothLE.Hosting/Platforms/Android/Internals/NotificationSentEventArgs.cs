using System;
using Android.Bluetooth;


namespace Shiny.BluetoothLE.Hosting.Internals
{
    public class NotificationSentEventArgs : GattEventArgs
    {
        public NotificationSentEventArgs(BluetoothDevice device, GattStatus status) : base(device)
        {
            this.Status = status;
        }


        public GattStatus Status { get; }
    }
}
