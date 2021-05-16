using System;


namespace Shiny.BluetoothLE
{
    public class BleConfiguration
    {
        /// <summary>
        /// Allows you to disable the internal sync queue
        /// DO NOT CHANGE this if you don't know what this is!
        /// </summary>
        public bool AndroidUseInternalSyncQueue { get; set; } = true;

        /// <summary>
        /// If you disable this, you need to manage serial/sequential access to ALL bluetooth operations yourself!
        /// DO NOT CHANGE this if you don't know what this is!
        /// </summary>
        public bool AndroidShouldInvokeOnMainThread { get; set; }

        /// <summary>
        /// Time span to pause before service discovery (helps in combating GATT133 error) when service discovery is performed immediately after connection
        /// DO NOT CHANGE this if you don't know what this is!
        /// </summary>
        public TimeSpan AndroidPauseBeforeServiceDiscovery { get; set; } = TimeSpan.FromMilliseconds(750);

        /// <summary>
        /// This will display an alert dialog when the user powers off their bluetooth adapter
        /// </summary>
        public bool iOSShowPowerAlert { get; set; }


        /// <summary>
        /// CBCentralInitOptions restoration key for background restoration
        /// </summary>
        public string iOSRestoreIdentifier { get; set; }
    }
}
