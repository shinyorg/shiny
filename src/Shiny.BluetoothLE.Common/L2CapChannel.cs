using System;
using System.Reactive;

namespace Shiny.BluetoothLE;


/// <summary>
/// An open L2CAP Connection-Oriented Channel for streaming bytes to/from the remote peer.
/// </summary>
/// <param name="Psm">The protocol/service multiplexer the channel was opened on.</param>
/// <param name="Identifier">
/// A platform-specific identifier for the remote peer (peripheral UUID on Apple, MAC address on Android).
/// </param>
/// <param name="Write">Function used to write bytes to the remote endpoint. Returns an observable that completes when the bytes have been queued.</param>
/// <param name="DataReceived">Hot observable of bytes received from the remote endpoint. Completes when the channel is closed.</param>
/// <param name="OnDispose">Optional disposal hook invoked when the channel is closed via <see cref="Dispose"/>.</param>
public record L2CapChannel(
    ushort Psm,
    string Identifier,
    Func<byte[], IObservable<Unit>> Write,
    IObservable<byte[]> DataReceived,
    Action? OnDispose = null
) : IDisposable
{
    /// <summary>
    /// Closes the channel and releases any platform resources.
    /// </summary>
    public void Dispose()
    {
        // drops the buffered reader the file transfer helpers attach on first use
        Infrastructure.L2CapChannelReader.Release(this);
        this.OnDispose?.Invoke();
    }
}
