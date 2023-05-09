using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE;


public interface ICanRequestMtu : IPeripheral
{
    /// <summary>
    /// Negotiates a requested MTU with the peripheral
    /// </summary>
    /// <param name="requestValue"></param>
    /// <returns></returns>
    IObservable<int> RequestMtu(int requestValue); 
}


public static class FeatureMtu
{
    /// <summary>
    /// Checks if MTU requests are available
    /// </summary>
    /// <param name="peripheral"></param>
    /// <returns></returns>
    public static bool CanRequestMtu(this IPeripheral peripheral) => peripheral is ICanRequestMtu;
    
    
    /// <summary>
    /// Requests MTU value if available to platform, otherwise returns the current negotiated MTU
    /// </summary>
    /// <param name="peripheral"></param>
    /// <param name="requestedValue"></param>
    /// <returns></returns>
    public static IObservable<int> TryRequestMtu(this IPeripheral peripheral, int requestedValue)
    {
        if (peripheral is ICanRequestMtu mtu)
            return mtu.RequestMtu(requestedValue);

        return Observable.Return(peripheral.Mtu);
    }


    /// <summary>
    /// Requests MTU value if available to platform, otherwise returns the current negotiated MTU
    /// </summary>
    /// <param name="peripheral"></param>
    /// <param name="requestedValue"></param>
    /// <param name="timeoutMillis"></param>
    /// <param name="cancelToken"></param>
    /// <returns></returns>
    public static Task<int> TryRequestMtuAsync(this IPeripheral peripheral, int requestedValue, int timeoutMillis = 5000, CancellationToken cancelToken = default)
        => peripheral
            .TryRequestMtu(requestedValue)
            .Timeout(TimeSpan.FromMilliseconds(timeoutMillis))
            .ToTask(cancelToken);
}

