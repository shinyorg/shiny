using System;


namespace Shiny.BluetoothLE.Central
{
    public interface IScanResult
    {
        int Rssi { get; }
        IPeripheral Peripheral { get; }
        IAdvertisementData AdvertisementData { get; }
    }
}
