using System;
using System.Linq;
using System.Reactive.Linq;


namespace Shiny.BluetoothLE
{
    public class DeviceInfo
    {
        public string? ManufacturerName { get; set; }
        public string? ModelNumber { get; set; }
        public string? FirmwareRevision { get; set; }
    }


    public static class CommonCharacteristicsExtensions
    {
        public const string DeviceInformationServiceUuid = "180A";
        public const string BatteryServiceUuid = "";


        public static IObservable<DeviceInfo> ReadDeviceInformation(this IPeripheral peripheral)
            => peripheral
                .GetKnownService(DeviceInformationServiceUuid)
                .Select(x => x.DiscoverCharacteristics())
                .Switch()
                .SelectMany(x => x.Read())
                .Buffer(3)
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

                            case '6':
                                dev.FirmwareRevision = BitConverter.ToString(item.Data);
                                break;

                            case '9':
                                dev.ManufacturerName = BitConverter.ToString(item.Data);
                                break;
                        }
                    }
                    return dev;
                });


        public static IObservable<int> ReadBatteryInformation(this IPeripheral peripheral) => peripheral
            .GetKnownService(BatteryServiceUuid)
            .Select(x => x.DiscoverCharacteristics())
            .Take(1)
            .Switch()
            .Select(x => x.Read())
            .Switch()
            .Select(x => (int)x.Data[0]);
    }
}
