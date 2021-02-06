using System;
using System.Threading.Tasks;


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


    public abstract class BleDelegate : IBleDelegate
    {
        public virtual Task OnAdapterStateChanged(AccessState state) => Task.CompletedTask;
        public virtual Task OnConnected(IPeripheral peripheral) => Task.CompletedTask;
        public virtual Task OnScanResult(ScanResult result) => Task.CompletedTask;
    }


    //public interface BleBetterDelegate
    //{
    //    Task OnAdapterStateChanged(AccessState state);
    //    Task OnConnected(IPeripheral peripheral);

    //Task<string> IdentifyDevice(IPeripheral peripheral, ScanResult result);
    //    // this would work well with the UWP backgrounding and allow the framework to deal with auto-reconnection - would have to know how to id device and store what characteristics to rehook
    //    Task OnCharacteristicHooked(IGattCharacteristic characteristic);
    //    Task OnCharacteristicNotification(CharacteristicGattResult result);
    //}
}