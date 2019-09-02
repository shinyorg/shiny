using System;
using System.Threading.Tasks;


namespace Shiny.BluetoothLE.Central
{
    public interface IBlePeripheralDelegate
    {
        Task OnConnected(IPeripheral peripheral);
        //void OnConnectFailed(IPeripheral peripheral);
        //void OnAdvertised(IScanResult result);
    }
}