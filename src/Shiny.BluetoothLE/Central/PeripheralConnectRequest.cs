using System;


namespace Shiny.BluetoothLE.Central
{
    public class PeripheralConnectRequest
    {
        public PeripheralConnectRequest()
        {
        }

        public Guid Uuid { get; }
        public DateTime Timestamp { get; }
    }
}
