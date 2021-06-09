using System;


namespace Shiny.BluetoothLE
{
    public class ManufacturerData
    {
        //public ManufacturerData() {}
        public ManufacturerData(ushort companyId, byte[] data)
        {
            this.CompanyId = companyId;
            this.Data = data;
        }


        public ushort CompanyId { get; } // set; }
        public byte[] Data { get; } //set; }
    }
}
