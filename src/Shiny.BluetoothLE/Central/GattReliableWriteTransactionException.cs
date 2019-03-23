using System;


namespace Shiny.BluetoothLE.Central
{
    public class GattReliableWriteTransactionException : Exception
    {
        public GattReliableWriteTransactionException(string msg) : base(msg) { }
    }
}
