using System;


namespace Shiny.Devices.Tests.BluetoothLE
{
    public static class Constants
    {
        public static TimeSpan DeviceScanTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public static TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public static TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(10);

        public static string PeripheralName { get; } = "Bean+";
        //public static Guid DeviceUuid { get; } = new Guid(""); - 90:7B:F3:58:3E:7F (droid)
        public static Guid AdServiceUuid { get; }              = new Guid("A495FF10-C5B1-4B44-B512-1370F02D74DE");
        public static Guid ScratchServiceUuid { get; }         = new Guid("A495FF20-C5B1-4B44-B512-1370F02D74DE");

        public static Guid ScratchCharacteristicUuid1 { get; } = new Guid("A495FF21-C5B1-4B44-B512-1370F02D74DE");
        public static Guid ScratchCharacteristicUuid2 { get; } = new Guid("A495FF22-C5B1-4B44-B512-1370F02D74DE");
        public static Guid ScratchCharacteristicUuid3 { get; } = new Guid("A495FF23-C5B1-4B44-B512-1370F02D74DE");
        public static Guid ScratchCharacteristicUuid4 { get; } = new Guid("A495FF24-C5B1-4B44-B512-1370F02D74DE");
        public static Guid ScratchCharacteristicUuid5 { get; } = new Guid("A495FF25-C5B1-4B44-B512-1370F02D74DE");
    }
}
