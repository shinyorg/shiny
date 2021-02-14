using System;
using System.Linq;
using System.Reactive.Linq;


namespace Shiny.BluetoothLE
{
    public class DeviceInfo
    {
        public string? ManufacturerName { get; set; }
        public string? ModelNumber { get; set; }
        public string? SerialNumber { get; set; }
        public string? FirmwareRevision { get; set; }
        public string? HardwareRevision { get; set; }
        public string? SoftwareRevision { get; set; }
    }


    public static class CommonCharacteristicsExtensions
    {
        public const string DeviceInformationServiceUuid = "180A";
        public const string BatteryServiceUuid = "180F";


        public static IObservable<DeviceInfo> ReadDeviceInformation(this IPeripheral peripheral)
            => peripheral
                .GetKnownService(DeviceInformationServiceUuid, true)
                .SelectMany(x => x.GetCharacteristics())
                .SelectMany(x => x.Select(y => y.Read()))
                .Concat()
                .ToList()
                .Select(data =>
                {
                    var dev = new DeviceInfo();
                    foreach (var item in data)
                    {
                        switch (item.Characteristic.Uuid[3])
                        {
                            case '4':
                                dev.ModelNumber = BitConverter.ToString(item.Data);
                                break;

                            case '5':
                                dev.SerialNumber = BitConverter.ToString(item.Data);
                                break;

                            case '6':
                                dev.FirmwareRevision = BitConverter.ToString(item.Data);
                                break;

                            case '7':
                                dev.HardwareRevision = BitConverter.ToString(item.Data);
                                break;

                            case '8':
                                dev.SoftwareRevision = BitConverter.ToString(item.Data);
                                break;

                            case '9':
                                dev.ManufacturerName = BitConverter.ToString(item.Data);
                                break;
                        }
                    }
                    return dev;
                });


        public static IObservable<int> ReadBatteryInformation(this IPeripheral peripheral) => peripheral
            .GetKnownService(BatteryServiceUuid, true)
            .Select(x => x.GetCharacteristics())
            .Take(1)
            .Switch()
            .Select(x => x.FirstOrDefault())
            .Where(x => x != null)
            .Select(x => x.Read())
            .Switch()
            .Select(x => (int)x.Data[0]);
    }
}
