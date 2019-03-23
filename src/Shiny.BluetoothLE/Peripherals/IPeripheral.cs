using System;


namespace Shiny.BluetoothLE.Peripherals
{
    public interface IPeripheral
    {
        //string Identifier { get; }
        // I can get this on iOS and Droid
        Guid Uuid { get; }

        /// <summary>
        /// You can set any data you want here
        /// </summary>
        object Context { get; set; }
    }
}
