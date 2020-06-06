using System;


namespace Shiny.BluetoothLE
{
    public interface IScanResult
    {
        int Rssi { get; }
        IPeripheral Peripheral { get; }
        IAdvertisementData AdvertisementData { get; }
    }
}
