using System;


namespace Shiny.BluetoothLE
{
    public class AdvertisementServiceData
    {
        public AdvertisementServiceData(Guid serviceUuid, byte[] data)
        {
            this.Uuid = serviceUuid;
            this.Data = data;
        }


        public Guid Uuid { get; }
        public byte[] Data { get; }
    }
}
