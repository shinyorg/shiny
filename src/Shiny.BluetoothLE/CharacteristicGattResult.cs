using System;


namespace Shiny.BluetoothLE
{
    public enum GattCharacteristicResultType
    {
        Read,
        Write,
        WriteWithoutResponse,
        Notification
    }


    public class GattCharacteristicResult
    {
        public GattCharacteristicResult(IGattCharacteristic characteristic, byte[]? data, GattCharacteristicResultType type)
        {
            this.Characteristic = characteristic;
            this.Type = type;
            this.Data = data;
        }


        public IGattCharacteristic Characteristic { get; }
        public GattCharacteristicResultType Type { get; }
        public byte[]? Data { get; }
    }
}
