using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;
using Shiny.BluetoothLE;

namespace Shiny.Printers.BluetoothLE;


/// <summary>
/// An <see cref="IPrinterConnection"/> backed by a Shiny.BluetoothLE <see cref="IPeripheral"/>. Writes are
/// split into MTU-sized chunks (the reason BLE printing needs a real BLE stack) with an optional drain delay.
/// </summary>
public sealed class BlePrinterConnection(IPeripheral peripheral, BlePrinterConfig config) : IPrinterConnection, IDisposable
{
    readonly IPeripheral peripheral = peripheral ?? throw new ArgumentNullException(nameof(peripheral));
    readonly BlePrinterConfig config = config ?? throw new ArgumentNullException(nameof(config));

    /// <summary>The underlying BLE peripheral.</summary>
    public IPeripheral Peripheral => this.peripheral;

    public bool IsConnected => this.peripheral.Status == ConnectionState.Connected;

    public IObservable<PrinterConnectionState> WhenStatusChanged()
        => this.peripheral.WhenStatusChanged().Select(Map);


    /// <summary>Connects to the peripheral (auto-reconnect off for a fast, deterministic connect).</summary>
    public Task Connect(CancellationToken cancellationToken = default, TimeSpan? timeout = null)
        => this.peripheral.ConnectAsync(
            new ConnectionConfig(AutoConnect: false),
            cancellationToken,
            timeout ?? TimeSpan.FromSeconds(30)
        );


    /// <summary>Disconnects and releases the BLE connection. Always call this when finished.</summary>
    public void Disconnect() => this.peripheral.CancelConnection();

    /// <summary>Same as <see cref="Disconnect"/>.</summary>
    public void Dispose() => this.Disconnect();


    public async ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        if (!this.IsConnected)
            throw new InvalidOperationException("The printer is not connected.");

        var chunkSize = this.config.ChunkSize ?? Math.Clamp(this.peripheral.Mtu - 3, 20, 512);
        var withResponse = !this.config.WriteWithoutResponse;

        var offset = 0;
        while (offset < data.Length)
        {
            cancellationToken.ThrowIfCancellationRequested();

            var length = Math.Min(chunkSize, data.Length - offset);
            var chunk = data.Slice(offset, length).ToArray();

            await this.peripheral
                .WriteCharacteristic(this.config.ServiceUuid, this.config.WriteCharacteristicUuid, chunk, withResponse)
                .ToTask(cancellationToken)
                .ConfigureAwait(false);

            offset += length;

            if (this.config.InterChunkDelay > TimeSpan.Zero && offset < data.Length)
                await Task.Delay(this.config.InterChunkDelay, cancellationToken).ConfigureAwait(false);
        }
    }


    static PrinterConnectionState Map(ConnectionState state) => state switch
    {
        ConnectionState.Connected => PrinterConnectionState.Connected,
        ConnectionState.Connecting => PrinterConnectionState.Connecting,
        _ => PrinterConnectionState.Disconnected
    };
}
