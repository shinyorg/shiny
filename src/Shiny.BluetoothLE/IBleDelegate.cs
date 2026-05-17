using System.Threading.Tasks;

namespace Shiny.BluetoothLE;


/// <summary>
/// Receives BLE lifecycle callbacks that may fire while the app is in the foreground or background.
/// Register implementations with the BLE host builder.
/// </summary>
public interface IBleDelegate
{
    /// <summary>
    /// Invoked when the BLE adapter access state changes (e.g. user toggles Bluetooth or permission changes).
    /// </summary>
    /// <param name="state">The new adapter access state.</param>
    Task OnAdapterStateChanged(AccessState state);


    /// <summary>
    /// Invoked when a peripheral's connection state changes, including reconnects that happen in the background.
    /// </summary>
    /// <param name="peripheral">The peripheral whose state changed.</param>
    Task OnPeripheralStateChanged(IPeripheral peripheral);
}


/// <summary>
/// Convenience base class that provides no-op virtual implementations of <see cref="IBleDelegate"/>.
/// </summary>
public abstract class BleDelegate : IBleDelegate
{
    /// <inheritdoc />
    public virtual Task OnAdapterStateChanged(AccessState state) => Task.CompletedTask;

    /// <inheritdoc />
    public virtual Task OnPeripheralStateChanged(IPeripheral peripheral) => Task.CompletedTask;
}
