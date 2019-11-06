using System;


namespace Shiny.BluetoothLE.Central
{
    public interface ICanControlAdapterState
    {
        /// <summary>
        /// Toggles the bluetooth adapter on/off - returns true if successful
        /// Works only on Android
        /// </summary>
        /// <returns></returns>
        void SetAdapterState(bool enable);
    }


    public static class Feature_CentralManager
    {
        public static bool CanControlAdapterState(this ICentralManager centralManager) => centralManager is ICanControlAdapterState;


        public static bool TrySetAdapterState(this ICentralManager centralManager, bool enable)
        {
            if (centralManager is ICanControlAdapterState state)
            {
                state.SetAdapterState(enable);
                return true;
            }
            return false;
        }
    }
}
