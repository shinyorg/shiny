using System;
using Android.Bluetooth;


namespace Shiny.BluetoothLE.Hosting.Internals
{
    public class GattEventArgs : EventArgs
    {
        public GattEventArgs(BluetoothDevice device)
        {
            this.Device = device;
        }


        public BluetoothDevice Device { get; }
    }
}