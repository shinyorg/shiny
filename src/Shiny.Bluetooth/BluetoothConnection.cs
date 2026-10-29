namespace Shiny.Bluetooth;


/// <summary>
/// An open RFCOMM connection to a remote device.
/// </summary>
/// <remarks>
/// RFCOMM is a reliable byte stream, like TCP: message boundaries are not preserved, so frame your
/// own protocol (or read lines) on top of <see cref="Stream"/>. Dispose to disconnect. A remote
/// disconnect shows up as a read returning 0, or an <see cref="IOException"/> from a write.
/// </remarks>
public sealed class BluetoothConnection : IAsyncDisposable, IDisposable
{
    internal BluetoothConnection(IBluetoothDevice device, Guid serviceUuid, Stream stream)
    {
        this.Device = device;
        this.ServiceUuid = serviceUuid;
        this.Stream = stream;
    }


    /// <summary>The remote device. On an accepted connection the name is only filled in where the platform already knew it.</summary>
    public IBluetoothDevice Device { get; }

    /// <summary>The service this connection was opened to, or accepted on.</summary>
    public Guid ServiceUuid { get; }

    /// <summary>The bidirectional byte stream. Disposing it closes the connection too.</summary>
    public Stream Stream { get; }


    public void Dispose() => this.Stream.Dispose();
    public ValueTask DisposeAsync() => this.Stream.DisposeAsync();
}
