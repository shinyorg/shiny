using System;


namespace Shiny.Printers
{
    public class BlePrinterConfig
    {
        public BlePrinterConfig(Guid scanServiceUuid, Guid serviceUuid, Guid writeCharacteristicUuid)
        {
            this.ScanServiceUuid = scanServiceUuid;
            this.ServiceUuid = serviceUuid;
            this.WriteCharacteristicUuid = writeCharacteristicUuid;
        }


        public Guid ScanServiceUuid { get; }
        public Guid ServiceUuid { get; }
        public Guid WriteCharacteristicUuid { get; }
    }
}
