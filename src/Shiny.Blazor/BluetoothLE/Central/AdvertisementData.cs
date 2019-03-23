using System;


namespace Acr.BluetoothLE.Central
{
    public class AdvertisementData : IAdvertisementData
    {
        public string LocalName { get; }
        public bool IsConnectable { get; }
        public AdvertisementServiceData[] ServiceData { get; }
        public ManufacturerData[] ManufacturerData { get; }
        public Guid[] ServiceUuids { get; }
        public int TxPower { get; }
    }
}
