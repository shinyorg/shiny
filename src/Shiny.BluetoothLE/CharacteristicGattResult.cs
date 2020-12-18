using System;


namespace Shiny.BluetoothLE
{
    public enum CharacteristicResultType
    {
        Read,
        Write,
        WriteWithoutResponse,
        NotificationSubscribed,
        Notification
    }


    public class CharacteristicGattResult
    {
        public CharacteristicGattResult(IGattCharacteristic characteristic, byte[]? data, CharacteristicResultType type)
        {
            this.Characteristic = characteristic;
            this.Type = type;
            this.Data = data;
        }


        public IGattCharacteristic Characteristic { get; }
        public CharacteristicResultType Type { get; }
        public byte[]? Data { get; }
    }
}
