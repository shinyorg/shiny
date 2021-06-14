using System;
using System.Collections.Generic;

namespace Shiny.Beacons
{
    public class BeaconMonitorConfig
    {
        /// <summary>
        /// This is the BLE GATT Service UUIDs provided by the manufacturer NOT the proximity UUID from the iBeacon
        /// This is required for background scanning on Android
        /// </summary>
        public List<string> ScanServiceUuids { get; set; } = new List<string>();

        public BeaconMonitorConfig UseEstimote()
        {
            this.ScanServiceUuids.Add("FE9A");
            return this;
        }
    }
}
