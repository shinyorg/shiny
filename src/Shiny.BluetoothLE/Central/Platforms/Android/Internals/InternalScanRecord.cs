using System;
using System.Linq;
using System.Collections.Generic;
using System.Text;

namespace Shiny.BluetoothLE.Central.Internals
{
    public class InternalScanRecord
    {
        public static InternalScanRecord Parse(byte[] scanRecord)
        {
            var sr = new InternalScanRecord();
            var index = 0;
            var others = new List<AdRecord>();

            while (index < scanRecord.Length)
            {
                var len = scanRecord[index++];
                if (len == 0)
                    break;

                var type = scanRecord[index];
                if (type == 0)
                    break;

                var data = new byte[len - 1];
                Array.Copy(scanRecord, index + 1, data, 0, len - 1);

                switch ((AdvertisementRecordType)type)
                {
                    case AdvertisementRecordType.TxPowerLevel:
                        sr.TxPower = (sbyte)data[0];
                        break;

                    case AdvertisementRecordType.CompleteLocalName:
                        sr.LocalName = Encoding.UTF8.GetString(data, 0, data.Length);
                        break;

                    case AdvertisementRecordType.ShortLocalName:
                        if (sr.LocalName == null)
                            sr.LocalName = Encoding.UTF8.GetString(data, 0, data.Length);
                        break;

                    case AdvertisementRecordType.ServiceData128Bit:
                    case AdvertisementRecordType.ServiceData32Bit:
                    case AdvertisementRecordType.ServiceData16Bit:
                        sr.ServiceData.Add(data);
                        break;

                    case AdvertisementRecordType.ManufacturerSpecificData:
                        sr.ManufacturerData = data;
                        break;

                    default:
                        var rec = new AdRecord((AdvertisementRecordType)type, data);
                        others.Add(rec);
                        break;
                }
                index += len;
            }
            others
                .Where(x => x.Type.ToString().Contains("Uuid"))
                .Select(x => x.Data.ToGuid())
                .ToList()
                .ForEach(sr.ServiceUuids.Add);

            return sr;
        }


        public string LocalName { get; private set; }
        public byte[] ManufacturerData { get; private set; }
        public bool IsConnectable { get; private set; }
        public int TxPower { get; private set; }
        public IList<Guid> ServiceUuids { get; } = new List<Guid>();
        public List<byte[]> ServiceData { get; } = new List<byte[]>();
    }


    public class AdRecord
    {
        public AdRecord(AdvertisementRecordType type, byte[] data)
        {
            this.Data = data;
            this.Type = type;
        }


        public byte[] Data { get; }
        public AdvertisementRecordType Type { get; }
    }
}
