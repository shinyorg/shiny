using System;


namespace Acr.BluetoothLE.Central
{
    public partial class Peripheral : AbstractPeripheral
    {
        public override int MtuSize { get; } = 20;
    }
}