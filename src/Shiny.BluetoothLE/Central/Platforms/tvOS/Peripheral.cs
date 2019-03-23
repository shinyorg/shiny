using System;
using CoreBluetooth;


namespace Acr.BluetoothLE.Central
{
    public partial class Peripheral : AbstractPeripheral
    {
        public override int MtuSize => (int)this
            .Native
            .GetMaximumWriteValueLength(CBCharacteristicWriteType.WithResponse);
    }
}