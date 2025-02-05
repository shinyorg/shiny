using System;
using System.Collections.Generic;
using System.Linq;
using SR = Android.Bluetooth.LE.ScanResult;

namespace Shiny.BluetoothLE;


public class AdvertisementData : IAdvertisementData
{
    public SR Native { get; }


    public AdvertisementData(SR result)
    {
        this.Native = result;

        this.manufacturerData = new Lazy<ManufacturerData?>(() =>
        {
            var md = this.Native.ScanRecord?.ManufacturerSpecificData;
            if (md == null)
                return null;

            ushort manufacturerId = 0;
            var manuData = new List<byte>();
            var size = md.Size();
            
            for (var i = 0; i < size; i++)
            {
                var id = md.KeyAt(i);
                if (id > 0)
                {
                    var data = this.Native.ScanRecord!.GetManufacturerSpecificData(id);
                    if (manufacturerId == 0)
                    {
                        manufacturerId = (ushort)id;
                    }
                    else
                    {
                        // if manufacturerID is already set, this is part of the manudata but Android is being stupid with it
                        var idBytes = id <= Int16.MaxValue 
                            ? BitConverter.GetBytes((ushort)id) // 2 bytes
                            : BitConverter.GetBytes(id); // 4 bytes
                        
                        manuData.AddRange(idBytes);
                    }
                    if (data.Length > 0)
                        manuData.AddRange(data);
                }
            }

            if (manufacturerId == 0)
                return null;
            
            return new ManufacturerData(manufacturerId, manuData.ToArray());
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


    public string LocalName => this.Native.ScanRecord.DeviceName;
    public bool? IsConnectable
    {
        get
        {
            // if not Android8+, we don't know the state of connectable
            if (Android.OS.Build.VERSION.SdkInt < Android.OS.BuildVersionCodes.O)
                return null;

            return this.Native.IsConnectable;
        }
    }

    readonly Lazy<ManufacturerData?> manufacturerData;
    public ManufacturerData? ManufacturerData => this.manufacturerData.Value;

    readonly Lazy<string[]?> serviceUuids;
    public string[]? ServiceUuids => this.serviceUuids.Value;

    readonly Lazy<AdvertisementServiceData[]?> serviceData;
    public AdvertisementServiceData[]? ServiceData => this.serviceData.Value;

    public int? TxPower => this.Native.ScanRecord?.TxPowerLevel;
}