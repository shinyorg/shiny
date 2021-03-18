using System;


namespace Shiny.Tests.BluetoothLE
{
    public static class Constants
    {
        public static TimeSpan DeviceScanTimeout { get; set; } = TimeSpan.FromSeconds(10);
        public static TimeSpan ConnectTimeout { get; set; } = TimeSpan.FromSeconds(30);
        public static TimeSpan OperationTimeout { get; set; } = TimeSpan.FromSeconds(10);

        public static string PeripheralName { get; } = "ShinyTest";
        //public static string DeviceUuid { get; } = ""); - 90:7B:F3:58:3E:7F (droid)
        public static string AdServiceUuid { get; }              = "A495FF10-C5B1-4B44-B512-1370F02D74DE";
        public static string ScratchServiceUuid { get; }         = "A495FF20-C5B1-4B44-B512-1370F02D74DE";

        public static string ScratchCharacteristicUuid1 { get; } = "A495FF21-C5B1-4B44-B512-1370F02D74DE";
        public static string ScratchCharacteristicUuid2 { get; } = "A495FF22-C5B1-4B44-B512-1370F02D74DE";
        public static string ScratchCharacteristicUuid3 { get; } = "A495FF23-C5B1-4B44-B512-1370F02D74DE";
        public static string ScratchCharacteristicUuid4 { get; } = "A495FF24-C5B1-4B44-B512-1370F02D74DE";
        public static string ScratchCharacteristicUuid5 { get; } = "A495FF25-C5B1-4B44-B512-1370F02D74DE";
    }
}
