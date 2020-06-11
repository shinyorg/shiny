using System;


namespace Shiny.BluetoothLE
{
    public class BleWriteSegment
    {
        public BleWriteSegment(byte[] chunk, int position, int len)
        {
            this.Chunk = chunk;
            this.Position = position;
            this.TotalLength = len;
        }


        public byte[] Chunk { get; }
        public int Position { get; }
        public int TotalLength { get; }
    }
}
