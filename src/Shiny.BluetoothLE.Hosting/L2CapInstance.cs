using System;

namespace Shiny.BluetoothLE.Hosting;


public struct L2CapInstance : IDisposable
{
    readonly Action onDispose;


    public L2CapInstance(ushort psm, Action onDispose)
    {
        this.Psm = psm;
        this.onDispose = onDispose;
    }


    public ushort Psm { get; }
    public void Dispose() => this.onDispose.Invoke();
}
