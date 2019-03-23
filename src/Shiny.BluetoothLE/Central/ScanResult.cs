using System;


namespace Shiny.BluetoothLE.Central
{
    public class ScanResult : IScanResult
    {
        public ScanResult(IPeripheral peripheral, int rssi, IAdvertisementData adData)
        {
            this.Peripheral = peripheral;
            this.Rssi = rssi;
            this.AdvertisementData = adData;
        }


        public IPeripheral Peripheral { get; }
        public int Rssi { get; }
        public IAdvertisementData AdvertisementData { get; }
    }
}
