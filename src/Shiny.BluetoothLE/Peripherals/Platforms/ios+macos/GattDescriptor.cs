using System;
using CoreBluetooth;


namespace Shiny.BluetoothLE.Peripherals
{
    public class GattDescriptor : IGattDescriptor
    {
        readonly CBPeripheralManager manager;
        readonly CBMutableDescriptor native;


        public GattDescriptor(CBPeripheralManager manager, CBMutableDescriptor native)
        {
            this.manager = manager;
            this.native = native;
        }
    }
}
