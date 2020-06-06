using System;
using Windows.Devices.Bluetooth.GenericAttributeProfile;


namespace Shiny.BluetoothLE.Peripherals
{
    public class Peripheral : IPeripheral
    {
        public Peripheral(GattSession session)
        {
            this.Uuid = new Guid(session.DeviceId.Id);
        }


        public Guid Uuid { get; }
        public object Context { get; set; }
    }
}
