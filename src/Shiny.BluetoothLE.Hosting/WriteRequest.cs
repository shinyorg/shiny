using System;


namespace Shiny.BluetoothLE.Hosting
{
    public class WriteRequest
    {
        public WriteRequest(IGattCharacteristic characteristic,
                            IPeripheral peripheral,
                            byte[] data,
                            int offset,
                            bool isReplyNeeded)
        {
            this.Characteristic = characteristic;
            this.Peripheral = peripheral;
            this.Offset = offset;
            this.IsReplyNeeded = isReplyNeeded;
            this.Data = data;
        }


        public IGattCharacteristic Characteristic { get; }
        public IPeripheral Peripheral { get; }
        public int Offset { get; }
        public bool IsReplyNeeded { get; }
        public byte[] Data { get; }
    }
}