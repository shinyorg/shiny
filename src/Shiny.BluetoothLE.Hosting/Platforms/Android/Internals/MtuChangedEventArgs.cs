using System;
using Android.Bluetooth;

namespace Shiny.BluetoothLE.Hosting.Internals
{
    public class MtuChangedEventArgs : EventArgs
    {
        public MtuChangedEventArgs(BluetoothDevice device, int mtu)
        {
            this.Device = device;
            this.Mtu = mtu;
        }


        public BluetoothDevice Device { get; }
        public int Mtu { get; }
    }
}