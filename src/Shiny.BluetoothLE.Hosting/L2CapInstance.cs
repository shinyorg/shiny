using System;

namespace Shiny.BluetoothLE.Hosting;


/// <summary>
/// Disposable handle to a listening L2CAP PSM. Dispose to close the listener.
/// </summary>
public struct L2CapInstance : IDisposable
{
    readonly Action onDispose;


    /// <summary>
    /// Creates a new instance bound to the supplied PSM and disposal action.
    /// </summary>
    /// <param name="psm">The assigned protocol/service multiplexer.</param>
    /// <param name="onDispose">Action invoked when the instance is disposed.</param>
    public L2CapInstance(ushort psm, Action onDispose)
    {
        this.Psm = psm;
        this.onDispose = onDispose;
    }


    /// <summary>
    /// Gets the protocol/service multiplexer assigned by the platform.
    /// </summary>
    public ushort Psm { get; }

    /// <summary>
    /// Closes the L2CAP listener.
    /// </summary>
    public void Dispose() => this.onDispose.Invoke();
}
