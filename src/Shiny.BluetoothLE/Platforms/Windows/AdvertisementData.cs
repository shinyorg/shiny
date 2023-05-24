//using System;
//using System.Linq;
//using System.Runtime.InteropServices.WindowsRuntime;

//using Windows.Devices.Bluetooth.Advertisement;


//namespace Shiny.BluetoothLE
//{
//    public class AdvertisementData : IAdvertisementData
//    {
//        readonly BluetoothLEAdvertisementReceivedEventArgs adData;
//        readonly Lazy<string[]?> serviceUuids;
//        readonly Lazy<ManufacturerData?> manufacturerData;
//        readonly Lazy<int?> txPower;


//        public AdvertisementData(BluetoothLEAdvertisementReceivedEventArgs args)
//        {
//            this.adData = args;

//            this.manufacturerData = new Lazy<ManufacturerData?>(() => args
//                .Advertisement
//                .ManufacturerData
//                .Select(x => new ManufacturerData(x.CompanyId, x.Data.ToArray()))
//                .FirstOrDefault()
//            );
//            this.serviceUuids = new Lazy<string[]?>(() => args
//                .Advertisement
//                .ServiceUuids
//                .Select(x => x.ToString())
//                .ToArray()
//            );
//            this.txPower = new Lazy<int?>(() => args.Advertisement.GetTxPower());
//        }


//        public BluetoothLEAdvertisement Native => this.adData.Advertisement;
//        public ulong BluetoothAddress => this.adData.BluetoothAddress;
//        public string? LocalName => this.adData.Advertisement.LocalName;
//        public bool? IsConnectable => this.adData.AdvertisementType == BluetoothLEAdvertisementType.ConnectableDirected ||
//                                      this.adData.AdvertisementType == BluetoothLEAdvertisementType.ConnectableUndirected;

//        public AdvertisementServiceData[]? ServiceData { get; } = null;
//        public ManufacturerData? ManufacturerData => this.manufacturerData.Value;
//        public string[]? ServiceUuids => this.serviceUuids.Value;
//        public int? TxPower => this.txPower.Value;
//    }
//}


/*
 public static string GetDeviceName(this BluetoothLEAdvertisement adv)
        {
            var data = adv.GetSectionDataOrNull(BluetoothLEAdvertisementDataTypes.CompleteLocalName);
            if (data == null)
                return adv.LocalName;

            var name = Encoding.UTF8.GetString(data);
            return name;
        }


        public static sbyte GetTxPower(this BluetoothLEAdvertisement adv)
        {
            var data = adv.GetSectionDataOrNull(BluetoothLEAdvertisementDataTypes.TxPowerLevel);
            return data == null ? (sbyte)0 : (sbyte) data[0];
        }


        public static byte[] GetManufacturerSpecificData(this BluetoothLEAdvertisement adv)
            => adv.GetSectionDataOrNull(BluetoothLEAdvertisementDataTypes.ManufacturerSpecificData);


        static byte[] GetSectionDataOrNull(this BluetoothLEAdvertisement adv, byte recType)
        {
            var section = adv.DataSections.FirstOrDefault(x => x.DataType == recType);
            var data = section?.Data.ToArray();
            return data;
        }
 */