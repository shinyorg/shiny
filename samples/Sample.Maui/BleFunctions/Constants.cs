namespace Sample
{
    public static class Constants
    {


        //const string ServiceUuid = "FFF0";
        //public static string ScanServiceUuid { get; set;}
        public const string ScanPeripheralName  = "VEEPEAK";
        public const string WriteValue = "";


        public static readonly (string ServiceUuid, string CharacteristicUuid) WriteCharacteristic = (
            "FFF0",
            "FFF2"
        );

        public static readonly (string ServiceUuid, string CharacteristicUuid) ReadCharacteristic = (
            "",
            ""
        );

        public static readonly (string ServiceUuid, string CharacteristicUuid) NotifyCharacteristic = (
            "",
            ""
        );
    }
}
