using System;
using System.Threading.Tasks;


namespace Shiny.BluetoothLE.Hosting.Hubs
{
    public class ShinyBleHub
    {
        public HubContext Context { get; }
        public virtual Task OnConnected(IPeripheral peripheral) => Task.CompletedTask;
        public virtual Task OnDisconnected(IPeripheral peripheral) => Task.CompletedTask;
    }
}
