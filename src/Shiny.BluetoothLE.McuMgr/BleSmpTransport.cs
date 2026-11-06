using System.Reactive.Linq;
using System.Reactive.Threading.Tasks;

namespace Shiny.BluetoothLE.McuMgr;


/// <summary>
/// Options for <see cref="BleSmpTransport"/>.
/// </summary>
public sealed class BleSmpTransportOptions
{
    /// <summary>
    /// The ATT MTU to request on connect (Android only - iOS, macOS and Windows negotiate on their own). The
    /// device grants the smaller of this and its own limit. Default 517.
    /// </summary>
    public int RequestedMtu { get; set; } = 517;

    /// <summary>
    /// Writes frames with write-without-response (the default, and what the Zephyr SMP characteristic declares).
    /// Set false for firmware whose characteristic only accepts acknowledged writes.
    /// </summary>
    public bool WriteWithoutResponse { get; set; } = true;

    /// <summary>
    /// The connection config used when the transport has to connect or reconnect the peripheral.
    /// </summary>
    public ConnectionConfig ConnectionConfig { get; set; } = new(AutoConnect: false);
}


/// <summary>
/// The MCUmgr SMP transport over Bluetooth LE: requests are written to the SMP characteristic and responses arrive
/// as notifications on it. Frames larger than the MTU are split across writes, and responses larger than the MTU
/// are reassembled from several notifications using the length in the SMP header.
/// </summary>
public sealed class BleSmpTransport : ISmpTransport, IDisposable
{
    /// <summary>The MCUmgr SMP GATT service UUID.</summary>
    public const string ServiceUuid = "8d53dc1d-1db7-4cd3-868b-8a527460aa84";

    /// <summary>The MCUmgr SMP GATT characteristic UUID (write without response + notify).</summary>
    public const string CharacteristicUuid = "da2e7828-fbce-4e01-ae9e-261174997c48";

    readonly IPeripheral peripheral;
    readonly BleSmpTransportOptions options;
    readonly SemaphoreSlim readyLock = new(1, 1);
    readonly List<byte> buffer = new();
    readonly object sync = new();

    IDisposable? notifySub;
    IDisposable? statusSub;
    TaskCompletionSource<byte[]>? pending;
    byte pendingSequence;
    bool mtuRequested;


    /// <summary>
    /// Creates a transport for <paramref name="peripheral"/>. The peripheral is connected on first use if it is not
    /// already.
    /// </summary>
    public BleSmpTransport(IPeripheral peripheral, BleSmpTransportOptions? options = null)
    {
        this.peripheral = peripheral ?? throw new ArgumentNullException(nameof(peripheral));
        this.options = options ?? new BleSmpTransportOptions();
    }


    /// <summary>The underlying peripheral.</summary>
    public IPeripheral Peripheral => this.peripheral;

    /// <inheritdoc />
    public int MaxFrameSize => this.peripheral.Mtu;


    /// <inheritdoc />
    public async Task<byte[]> SendAsync(byte[] frame, CancellationToken cancellationToken)
    {
        if (frame.Length < SmpHeader.Size)
            throw new ArgumentException("The frame is smaller than an SMP header", nameof(frame));

        await this.EnsureReadyAsync(cancellationToken).ConfigureAwait(false);

        var tcs = new TaskCompletionSource<byte[]>(TaskCreationOptions.RunContinuationsAsynchronously);
        lock (this.sync)
        {
            this.buffer.Clear();
            this.pendingSequence = frame[6];
            this.pending = tcs;
        }

        try
        {
            using var reg = cancellationToken.Register(() => tcs.TrySetCanceled(cancellationToken));

            var chunkSize = Math.Max(20, this.peripheral.Mtu);
            var offset = 0;
            while (offset < frame.Length)
            {
                var length = Math.Min(chunkSize, frame.Length - offset);
                var chunk = frame.AsSpan(offset, length).ToArray();

                await this.peripheral
                    .WriteCharacteristic(ServiceUuid, CharacteristicUuid, chunk, !this.options.WriteWithoutResponse)
                    .ToTask(cancellationToken)
                    .ConfigureAwait(false);

                offset += length;
            }
            return await tcs.Task.ConfigureAwait(false);
        }
        finally
        {
            lock (this.sync)
            {
                if (this.pending == tcs)
                    this.pending = null;
            }
        }
    }


    /// <inheritdoc />
    public async Task ReconnectAsync(TimeSpan delay, TimeSpan timeout, CancellationToken cancellationToken)
    {
        this.ResetSubscriptions();

        if (this.peripheral.Status != ConnectionState.Disconnected)
        {
            try
            {
                // the device drops the link when it resets - give it a moment, then drop it ourselves
                await this.peripheral
                    .WhenDisconnected()
                    .Take(1)
                    .Timeout(TimeSpan.FromSeconds(10))
                    .ToTask(cancellationToken)
                    .ConfigureAwait(false);
            }
            catch (TimeoutException)
            {
                await this.peripheral.DisconnectAsync(cancellationToken).ConfigureAwait(false);
            }
        }

        if (delay > TimeSpan.Zero)
            await Task.Delay(delay, cancellationToken).ConfigureAwait(false);

        await this.peripheral
            .ConnectAsync(this.options.ConnectionConfig, cancellationToken, timeout)
            .ConfigureAwait(false);
    }


    /// <summary>
    /// Releases the notification subscription. The peripheral connection is left as it is - the app owns it.
    /// </summary>
    public void Dispose()
    {
        this.ResetSubscriptions();
        this.readyLock.Dispose();
    }


    async Task EnsureReadyAsync(CancellationToken cancellationToken)
    {
        await this.readyLock.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            if (this.notifySub != null && this.peripheral.Status == ConnectionState.Connected)
                return;

            this.ResetSubscriptions();
            await this.peripheral
                .ConnectAsync(this.options.ConnectionConfig, cancellationToken)
                .ConfigureAwait(false);

            if (!this.mtuRequested)
            {
                this.mtuRequested = true;
                await this.peripheral
                    .TryRequestMtuAsync(this.options.RequestedMtu, cancelToken: cancellationToken)
                    .ConfigureAwait(false);
            }

            // listen for the CCCD write before starting the subscription so it cannot be missed
            var subscribed = this.peripheral.WaitForCharacteristicSubscriptionAsync(ServiceUuid, CharacteristicUuid, cancellationToken);
            this.notifySub = this.peripheral
                .NotifyCharacteristic(ServiceUuid, CharacteristicUuid, false)
                .Subscribe(
                    x => this.OnData(x.Data),
                    ex => this.Fail(new McuMgrException("The SMP characteristic subscription failed", ex))
                );

            this.statusSub = this.peripheral
                .WhenStatusChanged()
                .Where(x => x == ConnectionState.Disconnected)
                .Subscribe(_ => this.Fail(new McuMgrException("The peripheral disconnected while waiting for an SMP response")));

            await subscribed
                .WaitAsync(TimeSpan.FromSeconds(10), cancellationToken)
                .ConfigureAwait(false);
        }
        catch
        {
            this.ResetSubscriptions();
            throw;
        }
        finally
        {
            this.readyLock.Release();
        }
    }


    internal void OnData(byte[]? data)
    {
        if (data == null || data.Length == 0)
            return;

        TaskCompletionSource<byte[]>? complete = null;
        byte[]? frame = null;

        lock (this.sync)
        {
            if (this.pending != null)
            {
                this.buffer.AddRange(data);
                var span = System.Runtime.InteropServices.CollectionsMarshal.AsSpan(this.buffer);
                var expected = SmpHeader.GetFrameLength(span);

                if (expected != null && this.buffer.Count >= expected.Value)
                {
                    frame = span[..expected.Value].ToArray();
                    this.buffer.Clear();

                    // a late answer to a request that already timed out - drop it and keep waiting
                    if (frame[6] == this.pendingSequence)
                        complete = this.pending;
                }
            }
        }
        complete?.TrySetResult(frame!);
    }


    void Fail(Exception exception)
    {
        TaskCompletionSource<byte[]>? tcs;
        lock (this.sync)
            tcs = this.pending;

        tcs?.TrySetException(exception);
    }


    void ResetSubscriptions()
    {
        this.notifySub?.Dispose();
        this.notifySub = null;
        this.statusSub?.Dispose();
        this.statusSub = null;
    }
}
