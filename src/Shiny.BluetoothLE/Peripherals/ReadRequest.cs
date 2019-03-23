using System;


namespace Shiny.BluetoothLE.Peripherals
{
    public class ReadRequest
    {
        public ReadRequest(IGattCharacteristic characteristic, IPeripheral peripheral, int offset)
        {
            this.Characteristic = characteristic;
            this.Peripheral = peripheral;
            this.Offset = offset;
        }


        public IGattCharacteristic Characteristic { get; }
        public int Offset { get; }
        public IPeripheral Peripheral { get; }
    }
}
