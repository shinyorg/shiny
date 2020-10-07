using System;
using Windows.Devices.Bluetooth.GenericAttributeProfile;


namespace Shiny.BluetoothLE.Hosting
{
    public class Peripheral : IPeripheral
    {
        public Peripheral(GattSession session)
        {
            this.Uuid = session.DeviceId.Id;
        }


        public string Uuid { get; }
        public object Context { get; set; }
    }
}
