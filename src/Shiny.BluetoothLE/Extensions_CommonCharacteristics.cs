using System;
using System.Linq;
using System.Reactive.Linq;


namespace Shiny.BluetoothLE
{
    public static partial class Extensions
    {
        public static IObservable<DeviceInfo> ReadDeviceInformation(this IPeripheral peripheral)
            => peripheral
                .GetKnownService(StandardUuids.DeviceInformationServiceUuid, true)
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


        public static IObservable<int> ReadBatteryInformation(this IPeripheral peripheral)
            => StandardIntObserable(peripheral, StandardUuids.BatteryService);

        public static IObservable<int> HeartRateSensor(this IPeripheral peripheral)
            => StandardIntObserable(peripheral, StandardUuids.HeartRateMeasurementSensor);


        static IObservable<int> StandardIntObserable(IPeripheral peripheral, (string ServiceUuid, string CharacteristicUuid) uuid) => peripheral
            .GetKnownCharacteristic(uuid.ServiceUuid, uuid.CharacteristicUuid, true)
            .Select(x => x.Read())
            .Switch()
            .Select(x => (int)x.Data[0]);
    }
}
