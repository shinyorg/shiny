using System;
using System.Reactive.Linq;


namespace Shiny.BluetoothLE
{
    public interface ICanControlAdapterState : IBleManager
    {
        /// <summary>
        /// Toggles the bluetooth adapter on/off - returns true if successful
        /// Works only on Android
        /// </summary>
        /// <returns></returns>
        IObservable<bool> SetAdapterState(bool enable);
    }


    public static class Feature_CentralManager
    {
        public static bool CanControlAdapterState(this IBleManager centralManager) => centralManager is ICanControlAdapterState;


        public static IObservable<bool> TrySetAdapterState(this IBleManager bleManager, bool enable)
        {
            var result = false;
            if (bleManager is ICanControlAdapterState state)
            {
                state.SetAdapterState(enable);
                result = true;
            }
            return Observable.Return(result);
        }
    }
}
