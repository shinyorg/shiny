using System;
using System.Text;


namespace Shiny.BluetoothLE.Hosting
{
    public static class Extensions
    {
        /// <summary>
        /// WARNING: mobile devices often use the default service UUID in which case the OS will either error or ignore - change the UUID for testing
        /// </summary>
        /// <param name="manager"></param>
        public static void AddDeviceInfoService(this IBleHostingManager manager, DeviceInfo deviceInfo, string deviceInfoServiceUuid = StandardUuids.DeviceInformationServiceUuid) =>
            manager.AddService(
                deviceInfoServiceUuid,
                false,
                sb =>
                {
                    sb.AddReadCharacteristic(
                        "2A24",
                        () => Convert(deviceInfo.ModelNumber ?? "NO Model")
                    );
                    sb.AddReadCharacteristic(
                        "2A25",
                        () => Convert(deviceInfo.SerialNumber ?? "NO Serial")
                    );
                    sb.AddReadCharacteristic(
                        "2A26",
                        () => Convert(deviceInfo.FirmwareRevision ?? "NO Firmware Rev")
                    );
                    sb.AddReadCharacteristic(
                        "2A27",
                        () => Convert(deviceInfo.HardwareRevision ?? "NO HW Rev")
                    );
                    sb.AddReadCharacteristic(
                        "2A28",
                        () => Convert(deviceInfo.SoftwareRevision ?? "NO SW Rev")
                    );
                    sb.AddReadCharacteristic(
                        "2A29",
                        () => Convert(deviceInfo.ManufacturerName ?? "NO Manufacturer")
                    );
                }
            );


        /// <summary>
        /// WARNING: will attempt to use the default battery service UUID if not set, most OS' will ignore this due to having it already
        /// </summary>
        /// <param name="manager"></param>
        /// <param name="getBattery"></param>
        /// <param name="batteryServiceUuid"></param>
        public static void AddBatteryService(this IBleHostingManager manager, Func<int> getBattery, string? batteryServiceUuid = null) =>
            manager.AddService(
                batteryServiceUuid ?? StandardUuids.BatteryService.ServiceUuid,
                false,
                sb => sb.AddReadCharacteristic(StandardUuids.BatteryService.CharacteristicUuid, () => BitConverter.GetBytes(getBattery()))
            );


        public static IGattCharacteristic AddReadCharacteristic(this IGattServiceBuilder builder, string uuid, Func<byte[]> readRequest) =>
            builder.AddCharacteristic(uuid, cb => cb.SetRead(request => ReadResult.Success(readRequest())));


        public static IGattCharacteristic AddWriteCharacteristic(this IGattServiceBuilder builder, string uuid, Action<byte[]> onWrite) =>
            builder.AddCharacteristic(uuid, cb => cb.SetWrite(request =>
            {
                onWrite(request.Data);
                return GattState.Success;
            }));


        public static byte[]? Convert(object obj)
        {
            if (obj == null)
                return null;

            if (obj is string s)
                return Encoding.UTF8.GetBytes(s);

            if (obj is byte b)
                return new [] { b };

            if (obj is DateTimeOffset dt)
                return BitConverter.GetBytes(dt.ToUnixTimeSeconds());

            if (obj is int i)
                return BitConverter.GetBytes(i);

            throw new ArgumentException("Invalid Type - " + obj.GetType());
        }
    }
}
