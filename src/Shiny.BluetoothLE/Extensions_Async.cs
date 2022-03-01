using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;


namespace Shiny.BluetoothLE
{
    public static partial class Extensions
    {
        public static Task ConnectAsync(this IPeripheral peripheral, ConnectionConfig? config = null, CancellationToken cancelToken = default, TimeSpan? timeout = null)
            => peripheral
                .WithConnectIf(config)
                .Timeout(timeout ?? TimeSpan.FromSeconds(30))
                .ToTask(cancelToken);


        public static Task<IList<IGattService>> GetServicesAsync(this IPeripheral peripheral, CancellationToken cancelToken = default)
            => peripheral
                .GetServices()
                .ToTask(cancelToken);


        public static Task<IList<IGattCharacteristic>> GetCharacteristicsAsync(this IGattService service, CancellationToken cancelToken = default)
            => service
                .GetCharacteristics()
                .ToTask(cancelToken);


        public static Task<IList<IGattCharacteristic>> GetAllCharacteristicsAsync(this IPeripheral peripheral, CancellationToken cancelToken = default)
            => peripheral
                .GetAllCharacteristics()
                .ToTask(cancelToken);


        public static Task<IGattCharacteristic> GetKnownCharacteristicAsync(this IPeripheral peripheral, string serviceUuid, string characteristicUuid, CancellationToken cancelToken = default)
            => peripheral
                .GetKnownCharacteristic(serviceUuid, characteristicUuid)
                .Take(1)
                .ToTask(cancelToken);


        public static Task WriteBlobAsync(this IGattCharacteristic characteristic, Stream stream, TimeSpan? sendPacketTimeout = null, CancellationToken cancelToken = default)
            => characteristic.WriteBlob(stream, sendPacketTimeout).ToTask(cancelToken);


        public static Task<GattCharacteristicResult> WriteAsync(this IGattCharacteristic characteristic, byte[] data, bool withResponse, CancellationToken cancelToken = default)
            => characteristic.Write(data, withResponse).ToTask(cancelToken);


        public static Task<GattCharacteristicResult> ReadAsync(this IGattCharacteristic characteristic, CancellationToken cancelToken = default)
            => characteristic.Read().ToTask(cancelToken);


        public static Task<GattCharacteristicResult> WriteCharacteristicAsync(this IPeripheral peripheral, string serviceUuid, string characteristicUuid, byte[] data, bool withResponse = true, CancellationToken cancelToken = default)
            => peripheral.WriteCharacteristic(serviceUuid, characteristicUuid, data, withResponse).ToTask(cancelToken);

        public static Task<byte[]?> ReadCharacteristicAsync(this IPeripheral peripheral, string serviceUuid, string characteristicUuid, CancellationToken cancelToken = default)
            => peripheral.ReadCharacteristic(serviceUuid, characteristicUuid).ToTask(cancelToken);
    }
}
