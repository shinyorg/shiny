using System;


namespace Shiny.BluetoothLE
{
    public class BleException : Exception
    {
        public BleException(string message) : base(message) { }
        public BleException(string message, Exception exception) : base(message, exception) { }
    }
}
