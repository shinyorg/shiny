using System;


namespace Shiny.BluetoothLE.Central
{
    public class CharacteristicGattResult
    {
        public CharacteristicGattResult(IGattCharacteristic characteristic, byte[] data)
        {
            this.Characteristic = characteristic;
            this.Data = data;
        }


        public IGattCharacteristic Characteristic { get; }
        public byte[] Data { get; }
    }
}
