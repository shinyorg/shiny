using System;
using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.BluetoothLE;


/// <summary>
/// Optional peripheral capability for negotiating a custom GATT MTU size with the remote device.
/// </summary>
public interface ICanRequestMtu : IPeripheral
{
    /// <summary>
    /// Negotiates a new MTU with the peripheral. The actual MTU granted by the peer may be lower than the requested value.
    /// </summary>
    /// <param name="requestValue">The desired MTU in bytes.</param>
    /// <returns>A single-value observable with the negotiated MTU.</returns>
    IObservable<int> RequestMtu(int requestValue);
}


/// <summary>
/// Helpers for working with the optional <see cref="ICanRequestMtu"/> capability.
/// </summary>
public static class FeatureMtu
{
    /// <summary>
    /// Returns true when the peripheral supports MTU negotiation on the current platform.
    /// </summary>
    /// <param name="peripheral">The peripheral to test.</param>
    /// <returns>True when MTU negotiation is available; otherwise false.</returns>
    public static bool CanRequestMtu(this IPeripheral peripheral) => peripheral is ICanRequestMtu;


    /// <summary>
    /// Requests a new MTU if supported; otherwise returns the currently negotiated MTU.
    /// </summary>
    /// <param name="peripheral">The peripheral to negotiate with.</param>
    /// <param name="requestedValue">The desired MTU in bytes.</param>
    /// <returns>A single-value observable with the negotiated or current MTU.</returns>
    public static IObservable<int> TryRequestMtu(this IPeripheral peripheral, int requestedValue)
    {
        if (peripheral is ICanRequestMtu mtu)
            return mtu.RequestMtu(requestedValue);

        return Observable.Return(peripheral.Mtu);
    }


    /// <summary>
    /// Awaitable form of <see cref="TryRequestMtu"/> that times out if the peripheral does not respond.
    /// </summary>
    /// <param name="peripheral">The peripheral to negotiate with.</param>
    /// <param name="requestedValue">The desired MTU in bytes.</param>
    /// <param name="timeoutMillis">Timeout for the negotiation in milliseconds.</param>
    /// <param name="cancelToken">Optional cancellation token.</param>
    /// <returns>The negotiated or current MTU.</returns>
    public static Task<int> TryRequestMtuAsync(this IPeripheral peripheral, int requestedValue, int timeoutMillis = 5000, CancellationToken cancelToken = default)
        => peripheral
            .TryRequestMtu(requestedValue)
            .Timeout(TimeSpan.FromMilliseconds(timeoutMillis))
            .ToTask(cancelToken);
}
