using System;


namespace Shiny.BluetoothLE.Central
{
    public interface IBleStateRestoreDelegate
    {
        void OnConnected(IPeripheral peripheral);
        void OnAdvertised(IScanResult result);
    }
}