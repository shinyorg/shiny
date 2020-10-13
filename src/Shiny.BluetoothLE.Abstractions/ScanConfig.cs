using System;
using System.Collections.Generic;


namespace Shiny.BluetoothLE
{
    public class ScanConfig
    {
        /// <summary>
        /// Scan types - balanced & low latency are supported only on android
        /// </summary>
        public BleScanType ScanType { get; set; } = BleScanType.Balanced;


        /// <summary>
        /// Allows the use of Scan Batching, if supported by the underlying provider
        /// Currently, this only affects Android peripherals
        /// It defaults to false to be transparent/non-breaking with existing code
        /// </summary>
        public bool AndroidUseScanBatching { get; set; }


        /// <summary>
        /// Filters scan to peripherals that advertise specified service UUIDs
        /// iOS - you must set this to initiate a background scan
        /// </summary>
        public List<string> ServiceUuids { get; set; } = new List<string>();
    }
}
