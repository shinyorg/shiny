using System;


namespace Shiny.BluetoothLE.Flows
{
    public interface IBleFlow : IAsyncDisposable
    {
        IObservable<byte[]> Write(byte[] data);
    }
}
