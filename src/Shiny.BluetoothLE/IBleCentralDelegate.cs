using System;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny.BluetoothLE.Central
{
    public interface IBleCentralDelegate : IShinyDelegate
    {
        Task OnAdapterStateChanged(AccessState state);
        Task OnConnected(IPeripheral peripheral);
        //void OnConnectFailed(IPeripheral peripheral);
        //void OnAdvertised(IScanResult result);
    }
}