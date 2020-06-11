using System;


namespace Shiny.BluetoothLE
{
    public interface ICanRequestMtu
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


        public static IObservable<int> RequestMtu(this IPeripheral peripheral, int requestSize)
        {
            if (peripheral is ICanRequestMtu mtu)
                return mtu.RequestMtu(requestSize);

            throw new NotSupportedException("MTU requests are not supported on this platform");
        }


        public static IObservable<int> WhenMtuChanged(this IPeripheral periperhal)
        {
            if (periperhal is ICanRequestMtu mtu)
                return mtu.WhenMtuChanged();

            throw new NotSupportedException("MTU requests are not supported on this platform");
        }
    }
}
