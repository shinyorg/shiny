using System;
using System.Linq;
using SR = Android.Bluetooth.LE.ScanResult;

namespace Shiny.BluetoothLE;


public class AdvertisementData : IAdvertisementData
{
    readonly SR result;


    public AdvertisementData(SR result)
    {
        this.result = result;

        this.manufacturerData = new Lazy<ManufacturerData?>(() =>
        {
            var md = this.result.ScanRecord?.ManufacturerSpecificData;
            if (md == null || md.Size() == 0)
                return null;

            var manufacturerId = (ushort)md.KeyAt(0);
            if (manufacturerId == 0)
                return null;

            var data = this.result.ScanRecord!.GetManufacturerSpecificData(manufacturerId);
            return new ManufacturerData(manufacturerId, data!);
        });

        this.serviceUuids = new Lazy<string[]?>(() =>
            result
                .ScanRecord?
                .ServiceUuids?
                .Select(x => x.Uuid.ToString())
                .ToArray()
        );

        this.serviceData = new Lazy<AdvertisementServiceData[]?>(() =>
            result
                .ScanRecord?
                .ServiceData?
                .Select(x => new AdvertisementServiceData(x.Key.Uuid.ToString(), x.Value))
                .ToArray() ?? Array.Empty<AdvertisementServiceData>()
        );
    }


    public string LocalName => this.result.ScanRecord.DeviceName;
    public bool? IsConnectable
    {
        get
        {
            // if not Android8+, we don't know the state of connectable
            if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.O)
                return null;

            return this.result.IsConnectable;
        }
    }

    readonly Lazy<ManufacturerData?> manufacturerData;
    public ManufacturerData? ManufacturerData => this.manufacturerData.Value;

    readonly Lazy<string[]?> serviceUuids;
    public string[]? ServiceUuids => this.serviceUuids.Value;

    readonly Lazy<AdvertisementServiceData[]?> serviceData;
    public AdvertisementServiceData[]? ServiceData => this.serviceData.Value;

    public int? TxPower => this.result.ScanRecord?.TxPowerLevel;
}