using System;


namespace Shiny.BluetoothLE
{
    public class AdvertisementServiceData
    {
        public AdvertisementServiceData(string serviceUuid, byte[] data)
        {
            this.Uuid = serviceUuid;
            this.Data = data;
        }


        public string Uuid { get; }
        public byte[] Data { get; }
    }
}
