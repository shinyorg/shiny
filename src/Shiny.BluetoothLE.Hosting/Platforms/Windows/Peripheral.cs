using System;
using Windows.Devices.Bluetooth.GenericAttributeProfile;


namespace Shiny.BluetoothLE.Hosting
{
    public class Peripheral : IPeripheral
    {
        int mtu = 20; // Default MTU size from BLE spec


        public Peripheral(GattSession session)
        {
            this.Uuid = session.DeviceId.Id;
            this.mtu = Convert.ToInt32(session.MaxPduSize);
        }


        public string Uuid { get; }
        public object Context { get; set; }
        public int Mtu => this.mtu;
    }
}
