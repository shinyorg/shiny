using System;


namespace Shiny.BluetoothLE.Hosting
{
    public interface IPeripheral
    {
        //string Identifier { get; }
        // I can get this on iOS and Droid
        string Uuid { get; }

        /// <summary>
        /// You can set any data you want here
        /// </summary>
        object Context { get; set; }
    }
}
