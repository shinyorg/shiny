using System;
using System.Collections.Generic;


namespace Shiny.BluetoothLE.Hosting
{
    public class AdvertisementData
    {
        public ManufacturerData? ManufacturerData { get; set; }
        public List<string>? ServiceUuids { get; set; }
    }
}
