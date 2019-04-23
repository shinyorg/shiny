using System;


namespace Shiny.BluetoothLE
{
    public interface IBleAdapterDelegate
    {
        void OnBleAdapterStateChanged(AccessState state);
    }
}
