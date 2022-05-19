using System;
using System.Text;
using Microsoft.Extensions.Logging;

using Shiny.Hosting;

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
                        _ => Convert(deviceInfo.ModelNumber ?? "NO Model")!
                    );
                    sb.AddReadCharacteristic(
                        "2A25",
                        _ => Convert(deviceInfo.SerialNumber ?? "NO Serial")!
                    );
                    sb.AddReadCharacteristic(
                        "2A26",
                        _ => Convert(deviceInfo.FirmwareRevision ?? "NO Firmware Rev")!
                    );
                    sb.AddReadCharacteristic(
                        "2A27",
                        _ => Convert(deviceInfo.HardwareRevision ?? "NO HW Rev")!
                    );
                    sb.AddReadCharacteristic(
                        "2A28",
                        _ => Convert(deviceInfo.SoftwareRevision ?? "NO SW Rev")!
                    );
                    sb.AddReadCharacteristic(
                        "2A29",
                        _ => Convert(deviceInfo.ManufacturerName ?? "NO Manufacturer")!
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
                sb => sb.AddReadCharacteristic(StandardUuids.BatteryService.CharacteristicUuid, _ => BitConverter.GetBytes(getBattery()))
            );


        public static IGattCharacteristic AddReadCharacteristic(this IGattServiceBuilder builder, string uuid, Func<IPeripheral, byte[]> readRequest) =>
            builder.AddCharacteristic(uuid, cb => cb.SetRead(request =>
            {
                try
                {
                    var data = readRequest(request.Peripheral);
                    return ReadResult.Success(data);
                }
                catch (Exception ex)
                {
                    Host.Current.Logging.CreateLogger<IGattCharacteristic>().LogError("readRequest failed", ex);
                    return ReadResult.Error(GattState.Failure);
                }
            }));


        public static IGattCharacteristic AddWriteCharacteristic(this IGattServiceBuilder builder, string uuid, Action<IPeripheral, byte[]> onWrite) =>
            builder.AddCharacteristic(uuid, cb => cb.SetWrite(request =>
            {
                try
                {
                    onWrite(request.Peripheral, request.Data);
                    return GattState.Success;
                }
                catch (Exception ex)
                {
                    Host.Current.Logging.CreateLogger<IGattCharacteristic>().LogError("onWrite failed", ex);
                    return GattState.Failure;
                }
            }));


        public static byte[]? Convert(object obj)
        {
            if (obj == null)
                return null;

            if (obj is string s)
                return Encoding.UTF8.GetBytes(s);

            if (obj is byte b)
                return new[] { b };

            if (obj is DateTimeOffset dt)
                return BitConverter.GetBytes(dt.ToUnixTimeSeconds());

            if (obj is int i)
                return BitConverter.GetBytes(i);

            throw new ArgumentException("Invalid Type - " + obj.GetType());
        }
    }
}
