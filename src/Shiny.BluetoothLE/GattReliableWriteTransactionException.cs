using System;


namespace Shiny.BluetoothLE
{
    public class GattReliableWriteTransactionException : Exception
    {
        public GattReliableWriteTransactionException(string msg) : base(msg) { }
    }
}
