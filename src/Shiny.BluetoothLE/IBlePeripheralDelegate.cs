using System;


namespace Shiny.BluetoothLE.Central
{
    public interface IBlePeripheralDelegate
    {
        void OnConnected(IPeripheral peripheral);
        //void OnConnectFailed(IPeripheral peripheral);
        //void OnAdvertised(IScanResult result);
    }
}