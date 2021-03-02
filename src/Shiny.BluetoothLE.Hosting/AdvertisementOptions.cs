using System;
using System.Collections.Generic;


namespace Shiny.BluetoothLE.Hosting
{
    public class AdvertisementOptions
    {
        /// <summary>
        /// Used by UWP and iOS
        /// </summary>
        public string? LocalName { get; set; }

        /// <summary>
        /// WARNING: if your device name is too long, you will get an error
        /// </summary>
        public bool AndroidIncludeDeviceName { get; set; }
        public bool AndroidIncludeTxPower { get; set; } = true;

        /// <summary>
        /// Used by UWP and Android
        /// </summary>
        public ManufacturerData? ManufacturerData { get; set; }

        /// <summary>
        /// You must already have added gatt resources
        /// </summary>
        public bool UseGattServiceUuids { get; set; } = true;
        public List<string> ServiceUuids { get; set; } = new List<string>();
    }
}
