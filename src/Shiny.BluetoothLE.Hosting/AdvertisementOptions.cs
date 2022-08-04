using System;
using System.Collections.Generic;


namespace Shiny.BluetoothLE.Hosting
{
    public class AdvertisementOptions
    {
        /// <summary>
        /// Local name advertisement
        /// </summary>
        public string? LocalName { get; set; }

        /// <summary>
        /// You must already have added gatt resources
        /// </summary>
        public List<string> ServiceUuids { get; set; } = new List<string>();
    }
}
