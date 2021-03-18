using System;
using System.Reactive.Linq;

namespace Shiny.BluetoothLE
{
    public interface ICanRequestMtu : IPeripheral
    {
        IObservable<int> RequestMtu(int size);

        /// <summary>
        /// Fires when MTU size changes
        /// </summary>
        /// <returns>The mtu change requested.</returns>
        IObservable<int> WhenMtuChanged();
    }


    public static class FeatureExtensionsMtu
    {
        public static bool IsMtuRequestsAvailable(this IPeripheral peripheral) => peripheral is ICanRequestMtu;


        public static IObservable<int> TryRequestMtu(this IPeripheral peripheral, int requestSize)
        {
            if (peripheral is ICanRequestMtu mtu)
                return mtu.RequestMtu(requestSize);

            return Observable.Return(peripheral.MtuSize);
        }


        public static IObservable<int> WhenMtuChanged(this IPeripheral periperhal)
        {
            if (periperhal is ICanRequestMtu mtu)
                return mtu.WhenMtuChanged();

            return Observable.Empty<int>();
        }
    }
}
