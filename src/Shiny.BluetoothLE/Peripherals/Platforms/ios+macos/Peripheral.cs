using System;
using CoreBluetooth;


namespace Shiny.BluetoothLE.Peripherals
{
    public class Peripheral : IPeripheral
    {
        public Peripheral(CBCentral central)
        {
            this.Central = central;
            this.Uuid = new Guid(central.Identifier.ToString());
        }


        public Guid Uuid { get; }
        public CBCentral Central { get; }
        public object Context { get; set; }
    }
}