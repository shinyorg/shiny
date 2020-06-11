using System;
using Android.Bluetooth;


namespace Shiny.BluetoothLE.Internals
{
    public class GattCharacteristicEventArgs : GattEventArgs
    {
        public BluetoothGattCharacteristic Characteristic { get; }


        public GattCharacteristicEventArgs(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic) : this(gatt, characteristic, GattStatus.Success) {}


        public GattCharacteristicEventArgs(BluetoothGatt gatt, BluetoothGattCharacteristic characteristic, GattStatus status) : base(gatt, status)
        {
            this.Characteristic = characteristic;
        }
    }
}