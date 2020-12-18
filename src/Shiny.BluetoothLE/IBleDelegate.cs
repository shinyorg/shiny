using System;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny.BluetoothLE
{
    public interface IBleDelegate
    {
        /// <summary>
        /// Fires when the adapter state changes - foreground or background
        /// </summary>
        /// <param name="state"></param>
        /// <returns></returns>
        Task OnAdapterStateChanged(AccessState state);


        /// <summary>
        /// Fires when a device connects in the foreground or background
        /// </summary>
        /// <param name="peripheral"></param>
        /// <returns></returns>
        Task OnConnected(IPeripheral peripheral);


        /// <summary>
        /// This is normally used for background scan results
        /// </summary>
        /// <param name="result"></param>
        /// <returns></returns>
        Task OnScanResult(ScanResult result);
    }
}