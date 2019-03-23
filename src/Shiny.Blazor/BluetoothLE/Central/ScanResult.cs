using System;


namespace Acr.BluetoothLE.Central
{
    public class ScanResult : IScanResult
    {
        public int Rssi { get; }
        public IPeripheral Peripheral { get; }
        public IAdvertisementData AdvertisementData { get; }
    }
}
