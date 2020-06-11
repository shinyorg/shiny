using System;
using System.Collections.Generic;


namespace Shiny.BluetoothLE.Hosting
{
    public class AdvertisementData
    {
        public string? LocalName { get; set; }
        public ManufacturerData? ManufacturerData { get; set; }
        public List<Guid>? ServiceUuids { get; set; }
    }
}
