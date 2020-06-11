using System;
using Android.Bluetooth;


namespace Shiny.BluetoothLE.Internals
{
    public class ConnectionStateEventArgs : GattEventArgs
    {
        public ProfileState NewState { get; }


        public ConnectionStateEventArgs(BluetoothGatt gatt, GattStatus status, ProfileState newState) : base(gatt, status)
        {
            this.NewState = newState;
        }
    }
}