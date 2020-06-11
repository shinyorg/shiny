using System;
using Android.Bluetooth;


namespace Shiny.BluetoothLE.Hosting.Internals
{
    public class CharacteristicWriteEventArgs : WriteRequestEventArgs
    {
        public CharacteristicWriteEventArgs(
            BluetoothGattCharacteristic characteristic,
            BluetoothDevice device,
            int requestId,
            int offset,
            bool preparedWrite,
            bool responseNeeded,
            byte[] value) : base(device, requestId, offset, preparedWrite, responseNeeded, value)
        {
            this.Characteristic = characteristic;
        }


        public BluetoothGattCharacteristic Characteristic { get; }
    }
}