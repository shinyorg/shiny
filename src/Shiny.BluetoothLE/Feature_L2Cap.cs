using System;


namespace Shiny.BluetoothLE
{
    public interface IL2CapSupport
    {
        /// <summary>
        /// Open an L2Cap socket
        /// </summary>
        /// <param name="psm">PSM Value</param>
        /// <returns></returns>
        IObservable<IChannel> OpenChannel(int psm);
    }


    public static class FeatureL2Cap
    {
        public static bool IsL2CapAvailable(this IPeripheral peripheral) => peripheral is IL2CapSupport;


        public static IObservable<IChannel>? OpenChannel(this IPeripheral peripheral, int psm)
        {
            if (peripheral is IL2CapSupport support)
                return support.OpenChannel(psm);

            return null;
        }
    }
}
