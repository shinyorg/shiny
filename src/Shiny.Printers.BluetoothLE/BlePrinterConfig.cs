using System;


namespace Shiny.Printers
{
    public class BlePrinterConfig
    {
        public BlePrinterConfig(string scanServiceUuid, string serviceUuid, string writeCharacteristicUuid)
        {
            this.ScanServiceUuid = scanServiceUuid;
            this.ServiceUuid = serviceUuid;
            this.WriteCharacteristicUuid = writeCharacteristicUuid;
        }


        public string ScanServiceUuid { get; }
        public string ServiceUuid { get; }
        public string WriteCharacteristicUuid { get; }
    }
}
