using System;
using System.Threading.Tasks;


namespace Shiny.BluetoothLE
{
    public abstract class BleDelegate : IBleDelegate
    {
        public virtual Task OnAdapterStateChanged(AccessState state) => Task.CompletedTask;
        public virtual Task OnConnected(IPeripheral peripheral) => Task.CompletedTask;
        public virtual Task OnScanResult(ScanResult result) => Task.CompletedTask;
    }
}
