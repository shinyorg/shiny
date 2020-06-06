using System;


namespace Shiny.BluetoothLE
{
    public class ConnectionConfig
    {
        /// <summary>
        /// Android: Setting this to false will disable auto (re)connect when the peripheral
        /// is in range or when you disconnect.  However, it will speed up initial
        /// connections signficantly (defaults to true)
        /// iOS: Controls whether or not to reconnect automatically
        /// </summary>
        public bool AutoConnect { get; set; } = true;


        /// <summary>
        /// Control the android GATT connection priority
        /// </summary>
        public ConnectionPriority AndroidConnectionPriority { get; set; } = ConnectionPriority.Normal;
    }
}
