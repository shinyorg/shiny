using System;
using System.Linq;
using System.Runtime.InteropServices.WindowsRuntime;
using Windows.Devices.Bluetooth.Advertisement;

namespace Shiny.BluetoothLE;


public class AdvertisementData : IAdvertisementData
{
    readonly BluetoothLEAdvertisementReceivedEventArgs adData;
    readonly Lazy<string[]?> serviceUuids;
    readonly Lazy<ManufacturerData?> manufacturerData;
    readonly Lazy<AdvertisementServiceData[]?> serviceData;
    readonly Lazy<int?> txPower;


    public AdvertisementData(BluetoothLEAdvertisementReceivedEventArgs args)
    {
        this.adData = args;

        this.manufacturerData = new Lazy<ManufacturerData?>(() =>
        {
            var md = args.Advertisement.ManufacturerData.FirstOrDefault();
            if (md == null)
                return null;

            return new ManufacturerData(md.CompanyId, md.Data?.ToArray() ?? Array.Empty<byte>());
        });

        this.serviceUuids = new Lazy<string[]?>(() => args
            .Advertisement
            .ServiceUuids
            .Select(x => x.ToString())
            .ToArray()
        );

        this.serviceData = new Lazy<AdvertisementServiceData[]?>(() =>
        {
            var sections = args.Advertisement.GetSectionsByType(BluetoothLEAdvertisementDataTypes.ServiceData16BitUuids);
            if (sections == null || sections.Count == 0)
                return null;

            return sections
                .Select(x =>
                {
                    var data = x.Data?.ToArray() ?? Array.Empty<byte>();
                    if (data.Length < 2)
                        return null;

                    var uuid = $"0000{data[1]:X2}{data[0]:X2}-0000-1000-8000-00805F9B34FB";
                    var value = data.Length > 2 ? data.Skip(2).ToArray() : Array.Empty<byte>();
                    return new AdvertisementServiceData(uuid, value);
                })
                .Where(x => x != null)
                .ToArray()!;
        });

        this.txPower = new Lazy<int?>(() =>
        {
            var sections = args.Advertisement.GetSectionsByType(BluetoothLEAdvertisementDataTypes.TxPowerLevel);
            if (sections == null || sections.Count == 0)
                return null;

            var data = sections.First().Data?.ToArray();
            if (data == null || data.Length == 0)
                return null;

            return (sbyte)data[0];
        });
    }


    public string? LocalName => this.adData.Advertisement.LocalName;

    public bool? IsConnectable => this.adData.AdvertisementType == BluetoothLEAdvertisementType.ConnectableDirected ||
                                  this.adData.AdvertisementType == BluetoothLEAdvertisementType.ConnectableUndirected;

    public AdvertisementServiceData[]? ServiceData => this.serviceData.Value;
    public ManufacturerData? ManufacturerData => this.manufacturerData.Value;
    public string[]? ServiceUuids => this.serviceUuids.Value;
    public int? TxPower => this.txPower.Value;
}
