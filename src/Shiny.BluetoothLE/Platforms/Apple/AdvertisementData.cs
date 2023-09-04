using System;
using System.Collections.Generic;
using CoreBluetooth;
using Foundation;

namespace Shiny.BluetoothLE;


    public class AdvertisementData : IAdvertisementData
    {
        readonly NSDictionary adData;
        readonly Lazy<string> localName;
        readonly Lazy<bool> connectable;
        readonly Lazy<ManufacturerData?> manufacturerData;
        readonly Lazy<int> txpower;
        readonly Lazy<string[]> serviceUuids;
        readonly Lazy<AdvertisementServiceData[]> serviceData;


        public AdvertisementData(NSDictionary adData)
        {
            this.adData = adData;
            this.localName = this.GetLazy(CBAdvertisement.DataLocalNameKey, x => x.ToString());
            this.connectable = this.GetLazy(CBAdvertisement.IsConnectable, x => ((NSNumber)x).Int16Value == 1);
            this.txpower = this.GetLazy(CBAdvertisement.DataTxPowerLevelKey, x => Convert.ToInt32(((NSNumber)x).Int16Value));
            this.manufacturerData = this.GetLazy(CBAdvertisement.DataManufacturerDataKey, x =>
            {
                var data = ((NSData)x).ToArray();
                var len = (data?.Length ?? 0);
                if (len < 2)
                    return null;

                var companyId = ((data[1] & 0xFF) << 8) + (data[0] & 0xFF);

                if (len == 2)
                    return new ManufacturerData((ushort)companyId, Array.Empty<byte>());
                
                var value = new byte[data.Length - 2];
                Array.Copy(data, 2, value, 0, data.Length - 2);

                return new ManufacturerData((ushort)companyId, value);
            });
            this.serviceData = this.GetLazy(CBAdvertisement.DataServiceDataKey, item =>
            {
                var data = (NSDictionary)item;
                var list = new List<AdvertisementServiceData>();

                foreach (CBUUID key in data.Keys)
                {
                    var rawKey = key.Data.ToArray();
                    Array.Reverse(rawKey);

                    var rawValue = ((NSData)data.ObjectForKey(key)).ToArray();
                    list.Add(new AdvertisementServiceData(key.ToString(), rawValue));
                }
                return list.ToArray();
            });
            this.serviceUuids = this.GetLazy(CBAdvertisement.DataServiceUUIDsKey, x =>
            {
                var array = (NSArray)x;
                var list = new List<string>();
                for (nuint i = 0; i < array.Count; i++)
                {
                    var uuid = array.GetItem<CBUUID>(i).ToString();
                    list.Add(uuid);
                }
                return list.ToArray();
            });
        }


        public string? LocalName => this.localName.Value;
        public bool? IsConnectable => this.connectable.Value;
        public ManufacturerData? ManufacturerData => this.manufacturerData.Value;
        public string[]? ServiceUuids => this.serviceUuids.Value;
        public AdvertisementServiceData[]? ServiceData => this.serviceData.Value;
        public int? TxPower => this.txpower.Value;


        protected Lazy<T> GetLazy<T>(NSString key, Func<NSObject, T> transform) => new Lazy<T>(() =>
        {
            var obj = this.GetObject(key);
            if (obj == null)
                return default;

            var result = transform(obj);
            return result;
        });


        protected NSObject? GetObject(NSString key)
        {
            if (this.adData == null)
                return null;

            if (!this.adData.ContainsKey(key))
                return null;

            return this.adData.ObjectForKey(key);
        }
    }