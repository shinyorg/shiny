using System;
using System.Threading.Tasks;


namespace Shiny.BluetoothLE.Central
{
    public interface IBleCentralDelegate
    {
        Task OnAdapterStateChanged(AccessState state);
        Task OnConnected(IPeripheral peripheral);
        //void OnConnectFailed(IPeripheral peripheral);
        //void OnAdvertised(IScanResult result);
    }
}