using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;


namespace Shiny.BluetoothLE
{
    public static class AsyncExtensions
    {
        public static Task ConnectWaitAsync(this IPeripheral peripheral, CancellationToken? cancelToken = null)
            => peripheral
                .ConnectWait()
                .Timeout(TimeSpan.FromSeconds(30))
                .ToTask(cancelToken ?? CancellationToken.None);


        public static Task<IList<IGattService>> GetServicesAsync(this IPeripheral peripheral, CancellationToken? cancelToken = null)
            => peripheral
                .GetServices()
                .ToTask(cancelToken ?? CancellationToken.None);


        public static Task<IList<IGattCharacteristic>> GetCharacteristicsAsync(this IGattService service, CancellationToken? cancelToken = null)
            => service
                .GetCharacteristics()
                .ToTask(cancelToken ?? CancellationToken.None);


        public static Task<IList<IGattCharacteristic>> GetAllCharacteristicsAsync(this IPeripheral peripheral, CancellationToken? cancelToken = null)
            => peripheral
                .GetAllCharacteristics()
                .ToTask(cancelToken ?? CancellationToken.None);


        public static Task<IGattCharacteristic> GetKnownCharacteristicAsync(this IPeripheral peripheral, string serviceUuid, string characteristicUuid, CancellationToken? cancelToken = null)
            => peripheral
                .GetKnownCharacteristic(serviceUuid, characteristicUuid)
                .Take(1)
                .ToTask(cancelToken ?? CancellationToken.None);


        public static Task<GattCharacteristicResult> WriteAsync(this IGattCharacteristic characteristic, byte[] data, bool withResponse, CancellationToken? cancelToken = null)
            => characteristic.Write(data, withResponse).ToTask(cancelToken ?? CancellationToken.None);


        public static Task<GattCharacteristicResult> ReadAsync(this IGattCharacteristic characteristic, CancellationToken? cancelToken = null)
            => characteristic.Read().ToTask(cancelToken ?? CancellationToken.None);
    }
}
