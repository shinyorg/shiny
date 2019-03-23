using System;


namespace Shiny.BluetoothLE.Central
{
    public interface IAdvertisementData
    {
        string LocalName { get; }
        bool IsConnectable { get; }
        AdvertisementServiceData[] ServiceData { get; }
        ManufacturerData[] ManufacturerData { get; }
        Guid[] ServiceUuids { get; }
        int TxPower { get; }
    }
}
