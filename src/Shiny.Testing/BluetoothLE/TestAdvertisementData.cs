using System;
using Shiny.BluetoothLE;


namespace Shiny.Testing.BluetoothLE
{
    public class TestAdvertisementData : IAdvertisementData
    {
        public string? LocalName { get; set; }
        public bool? IsConnectable { get; set; } = true;
        public AdvertisementServiceData[]? ServiceData { get; set; }
        public ManufacturerData? ManufacturerData { get; set; }
        public string[]? ServiceUuids { get; set; }
        public int? TxPower { get; set; }
    }
}
