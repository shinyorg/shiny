using System;
using System.Linq;
using SR = Android.Bluetooth.LE.ScanResult;


namespace Shiny.BluetoothLE.Central
{
    public class AdvertisementData : IAdvertisementData
    {

        readonly SR result;
        public AdvertisementData(SR result)
        {
            this.result = result;

            this.manufacturerData = new Lazy<ManufacturerData[]>(() =>
            {
                var md = this.result.ScanRecord.ManufacturerSpecificData;
                var mdata = new ManufacturerData[md.Size()];
                //for (var i = 0; i < md.Size(); i++)
                //{
                //    var companyId = mdata.KeyAt(i);

                //    mdata[i] = null;
                //}

                return mdata;
            });

            this.serviceUuids = new Lazy<Guid[]>(() =>
                result
                    .ScanRecord
                    .ServiceUuids?
                    .Select(x => x.Uuid.ToGuid())
                    .ToArray()
            );

            this.serviceData = new Lazy<AdvertisementServiceData[]>(() =>
                result
                    .ScanRecord
                    .ServiceData
                    .Select(x => new AdvertisementServiceData(x.Key.Uuid.ToGuid(), x.Value))
                    .ToArray()
            );
        }



        public string LocalName => this.result.ScanRecord.DeviceName;
        public bool IsConnectable => this.result.IsConnectable; // only on droid8+

        readonly Lazy<ManufacturerData[]> manufacturerData;
        public ManufacturerData[] ManufacturerData => this.manufacturerData.Value;

        readonly Lazy<Guid[]> serviceUuids;
        public Guid[] ServiceUuids => this.serviceUuids.Value;

        readonly Lazy<AdvertisementServiceData[]> serviceData;
        public AdvertisementServiceData[] ServiceData => this.serviceData.Value;

        public int TxPower => this.result.ScanRecord.TxPowerLevel;
    }
}