using System.Threading.Tasks;

namespace Shiny.BluetoothLE;


public interface IBleDelegate
{
    /// <summary>
    /// Fires when the adapter state changes - foreground or background
    /// </summary>
    /// <param name="state"></param>
    /// <returns></returns>
    Task OnAdapterStateChanged(AccessState state);


    /// <summary>
    /// Fires when a device connects in the foreground or background
    /// </summary>
    /// <param name="peripheral"></param>
    /// <returns></returns>
    Task OnPeripheralStateChanged(IPeripheral peripheral);
}


public abstract class BleDelegate : IBleDelegate
{
    public virtual Task OnAdapterStateChanged(AccessState state) => Task.CompletedTask;
    public virtual Task OnPeripheralStateChanged(IPeripheral peripheral) => Task.CompletedTask;
}
