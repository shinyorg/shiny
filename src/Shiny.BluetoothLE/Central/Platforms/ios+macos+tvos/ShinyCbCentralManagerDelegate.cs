using System;
using CoreBluetooth;
using Foundation;


namespace Shiny.BluetoothLE.Central.Delegates
{
    public class ShinyCbCentralManagerDelegate : CBCentralManagerDelegate
    {
        readonly IMessageBus messageBus;

        public ShinyCbCentralManagerDelegate()
        {
            this.messageBus = ShinyHost.Resolve<IMessageBus>();
        }


        public override void ConnectedPeripheral(CBCentralManager central, CBPeripheral peripheral)
        {
        }


        public override void DisconnectedPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error)
        {
        }


        public override void DiscoveredPeripheral(CBCentralManager central, CBPeripheral peripheral, NSDictionary advertisementData, NSNumber RSSI)
        {
        }


        public override void FailedToConnectPeripheral(CBCentralManager central, CBPeripheral peripheral, NSError error)
        {
        }


        public override void RetrievedConnectedPeripherals(CBCentralManager central, CBPeripheral[] peripherals)
        {
        }


        public override void RetrievedPeripherals(CBCentralManager central, CBPeripheral[] peripherals)
        {
        }



        public override void WillRestoreState(CBCentralManager central, NSDictionary dict)
        {
            // background restore
        }


        public override void UpdatedState(CBCentralManager central)
        {
        }
    }
}
