using System;
using System.Linq;

namespace Shiny.BluetoothLE;


public class AdvertisementData(JsScanResult sr) : IAdvertisementData
{
    public string? LocalName => sr.DeviceName;
    public bool? IsConnectable => null;
    public string[]? ServiceUuids => sr.ServiceUuids;
    public int? TxPower => sr.TxPower == 0 ? null : sr.TxPower;


    /// <remarks>
    /// Only populated on the free-running scan path - the Web Bluetooth chooser reports no
    /// advertisement payload at all, so a chooser-discovered peripheral has none of this.
    /// </remarks>
    public AdvertisementServiceData[]? ServiceData => sr
        .ServiceData?
        .Select(x => new AdvertisementServiceData(x.Uuid ?? String.Empty, Decode(x.Data)))
        .ToArray();


    /// <remarks>
    /// Web Bluetooth exposes a map keyed by company id, but Shiny models a single manufacturer
    /// section - the first is taken, matching what the native implementations do.
    /// </remarks>
    public ManufacturerData? ManufacturerData
    {
        get
        {
            var first = sr.ManufacturerData?.FirstOrDefault();
            return first == null
                ? null
                : new ManufacturerData((ushort)first.CompanyId, Decode(first.Data));
        }
    }


    static byte[] Decode(string? base64)
        => base64.IsEmpty() ? [] : Convert.FromBase64String(base64!);
}
