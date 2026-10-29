using Shiny.Bluetooth.Infrastructure;
using Xunit;
namespace Shiny.Bluetooth.Tests;


public class BluetoothListenerTests
{
    static BluetoothConnection NewConnection(Stream? stream = null)
        => new(new BluetoothDevice(new FakeOperations(), "AA:BB:CC:DD:EE:FF"), BluetoothServiceUuids.SerialPort, stream ?? new MemoryStream());


    [Fact]
    public async Task Accept_ReturnsQueuedConnectionsInOrder()
    {
        var listener = new BluetoothListener(BluetoothServiceUuids.SerialPort, "Test", () => default);
        var first = NewConnection();
        var second = NewConnection();
        listener.Enqueue(first);
        listener.Enqueue(second);

        Assert.Same(first, await listener.Accept());
        Assert.Same(second, await listener.Accept());
    }


    [Fact]
    public async Task Dispose_StopsOnce_ClosesUnacceptedConnections_AndEndsAccept()
    {
        var stops = 0;
        var listener = new BluetoothListener(BluetoothServiceUuids.SerialPort, "Test", () =>
        {
            stops++;
            return default;
        });
        var orphan = new MemoryStream();
        listener.Enqueue(NewConnection(orphan));

        await listener.DisposeAsync();
        await listener.DisposeAsync();

        Assert.Equal(1, stops);
        Assert.False(orphan.CanRead); // disposed
        await Assert.ThrowsAsync<ObjectDisposedException>(() => listener.Accept());
    }


    [Fact]
    public async Task Enqueue_AfterDispose_ClosesTheConnection()
    {
        var listener = new BluetoothListener(BluetoothServiceUuids.SerialPort, "Test", () => default);
        await listener.DisposeAsync();

        var late = new MemoryStream();
        listener.Enqueue(NewConnection(late));
        Assert.False(late.CanRead);
    }


    [Fact]
    public async Task Fault_SurfacesAsBluetoothException()
    {
        var listener = new BluetoothListener(BluetoothServiceUuids.SerialPort, "Test", () => default);
        var pending = listener.Accept();
        listener.Fault(new IOException("adapter gone"));

        var ex = await Assert.ThrowsAsync<BluetoothException>(() => pending);
        Assert.IsType<IOException>(ex.InnerException);
    }
}
