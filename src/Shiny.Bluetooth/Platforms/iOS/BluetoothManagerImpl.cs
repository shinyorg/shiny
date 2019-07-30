using System;
using CoreBluetooth;
using Foundation;

namespace Shiny.Bluetooth
{
    public class BluetoothManagerImpl : CBCentralManagerDelegate, IBluetoothManager
    {
        readonly CBCentralManager manager;


        public BluetoothManagerImpl()
        {
            this.manager = new CBCentralManager();
        }


        public override void DiscoveredPeripheral(CBCentralManager central, CBPeripheral peripheral, NSDictionary advertisementData, NSNumber RSSI)
        {
        }

        public override void UpdatedState(CBCentralManager central)
        {
        }
    }
}
