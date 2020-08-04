using System;
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

        public static Task<IGattService> GetServicesAsync(this IPeripheral peripheral, CancellationToken? cancelToken = null)
            => peripheral.DiscoverServices().ToTask(cancelToken ?? CancellationToken.None);
    }
}
