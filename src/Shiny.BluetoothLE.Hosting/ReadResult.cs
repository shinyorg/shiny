using System;


namespace Shiny.BluetoothLE.Hosting
{
    public class ReadResult
    {
        public static ReadResult Success(byte[] data) => new ReadResult
        {
            Status = GattState.Success,
            Data = data
        };


        public static ReadResult Error(GattState status) => new ReadResult
        {
            Status = status
        };


        public GattState Status { get; private set; }
        public byte[] Data { get; private set; }
    }
}
