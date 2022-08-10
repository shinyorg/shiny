using System;


namespace Shiny.BluetoothLE
{
    public class DeviceInfo
    {
        public string? SystemId { get; set; }
        public string? ManufacturerName { get; set; }
        public string? ModelNumber { get; set; }
        public string? SerialNumber { get; set; }
        public string? FirmwareRevision { get; set; }
        public string? HardwareRevision { get; set; }
        public string? SoftwareRevision { get; set; }
    }
}
