using System;
using CoreBluetooth;


namespace Shiny.BluetoothLE.Central
{
    public partial class Peripheral : AbstractPeripheral
    {
        public override int MtuSize => (int)this
            .Native
            .GetMaximumWriteValueLength(CBCharacteristicWriteType.WithoutResponse);
    }
}