using System;
using CoreFoundation;


namespace Shiny.BluetoothLE.Central
{
    public class BleAdapterConfiguration
    {
        /// <summary>
        /// This will display an alert dialog when the user powers off their bluetooth adapter
        /// </summary>
        public bool ShowPowerAlert { get; set; }


        /// <summary>
        /// CBCentralInitOptions restoration key for background restoration
        /// </summary>
        public string RestoreIdentifier { get; set; }


        /// <summary>
        /// The scan dispatch queue to use - don't touch this if you don't know what it does
        /// </summary>
        public DispatchQueue DispatchQueue { get; set; }
    }
}
