using System.Collections.Generic;
using System.Linq;

namespace Shiny.BluetoothLE;


public class AdvertisementData : IAdvertisementData
{
    public AdvertisementData(
        string? localName,
        string[]? serviceUuids,
        int? txPower,
        ManufacturerData? manufacturerData,
        AdvertisementServiceData[]? serviceData)
    {
        this.LocalName = localName;
        this.ServiceUuids = serviceUuids;
        this.TxPower = txPower;
        this.ManufacturerData = manufacturerData;
        this.ServiceData = serviceData;
    }

    public string? LocalName { get; }
    public bool? IsConnectable => null; // BlueZ doesn't easily expose this
    public AdvertisementServiceData[]? ServiceData { get; }
    public ManufacturerData? ManufacturerData { get; }
    public string[]? ServiceUuids { get; }
    public int? TxPower { get; }


    internal static AdvertisementData FromDeviceProperties(
        string? name,
        string[]? uuids,
        short txPower,
        Dictionary<ushort, byte[]>? manufacturerData,
        Dictionary<string, byte[]>? serviceData)
    {
        ManufacturerData? mfData = null;
        if (manufacturerData != null && manufacturerData.Count > 0)
        {
            var first = manufacturerData.First();
            mfData = new ManufacturerData(first.Key, first.Value);
        }

        AdvertisementServiceData[]? svcData = null;
        if (serviceData != null && serviceData.Count > 0)
        {
            svcData = serviceData
                .Select(x => new AdvertisementServiceData(x.Key, x.Value))
                .ToArray();
        }

        return new AdvertisementData(
            name,
            uuids,
            txPower == 0 ? null : (int)txPower,
            mfData,
            svcData
        );
    }
}
