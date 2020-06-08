using System;


namespace Shiny.BluetoothLE
{
    public interface IChannel : IDisposable
    {
        Guid PeerUuid { get; }
        int Psm { get; } //=> 0x25;

        IStream InputStream { get; }
        IStream OutputStream { get; }
    }
}