using System;
using Windows.Devices.Enumeration;


namespace Shiny.BluetoothLE.Central
{
    public class DeviceInfoArgs
    {
        public DeviceInfoArgs(DeviceInformation dev, DeviceInfoStatus status)
        {
            this.Device = dev;
            this.Status = status;
        }


        public DeviceInformation Device { get; }
        public DeviceInfoStatus Status { get; }
    }


    public enum DeviceInfoStatus
    {
        Added,
        Removed
    }
}
