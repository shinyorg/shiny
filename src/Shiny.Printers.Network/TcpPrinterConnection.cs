using System.Net.Sockets;
using System.Reactive.Subjects;

namespace Shiny.Printers.Network;


/// <summary>
/// An <see cref="IPrinterConnection"/> backed by a raw TCP socket (port 9100 "RAW" / JetDirect). Unlike
/// BLE there is no MTU to honour - TCP handles fragmentation, so writes go out in a single streamed pass. The
/// socket is two-way, so it is also an <see cref="IPrinterQueryConnection"/> for status queries.
/// </summary>
public sealed class TcpPrinterConnection : IPrinterQueryConnection, IDisposable
{
    readonly string host;
    readonly int port;
    readonly TimeSpan connectTimeout;
    readonly BehaviorSubject<PrinterConnectionState> status = new(PrinterConnectionState.Disconnected);
    // one writer at a time - a status query must not land in the middle of a print job's bytes
    readonly SemaphoreSlim gate = new(1, 1);
    Socket? socket;


    public TcpPrinterConnection(string host, int port = 9100, TimeSpan? connectTimeout = null)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(host);
        this.host = host;
        this.port = port;
        this.connectTimeout = connectTimeout ?? TimeSpan.FromSeconds(10);
    }

    public TcpPrinterConnection(NetworkPrinterConfig config)
        : this(
            (config ?? throw new ArgumentNullException(nameof(config))).Host,
            config.Port,
            config.ConnectTimeout)
    {
    }


    /// <summary>The host this connection targets.</summary>
    public string Host => this.host;

    /// <summary>The port this connection targets.</summary>
    public int Port => this.port;

    public bool IsConnected => this.socket?.Connected ?? false;

    public IObservable<PrinterConnectionState> WhenStatusChanged() => this.status;


    /// <summary>Opens the TCP connection (dual-stack, Nagle disabled). Honours <paramref name="timeout"/> or the configured connect timeout.</summary>
    public async Task Connect(CancellationToken cancellationToken = default, TimeSpan? timeout = null)
    {
        if (this.IsConnected)
            return;

        this.status.OnNext(PrinterConnectionState.Connecting);

        // The two-arg ctor yields a dual-stack (IPv6 + mapped IPv4) socket - works for IPs and host names.
        var s = new Socket(SocketType.Stream, ProtocolType.Tcp) { NoDelay = true };
        using var linked = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        linked.CancelAfter(timeout ?? this.connectTimeout);

        try
        {
            await s.ConnectAsync(this.host, this.port, linked.Token).ConfigureAwait(false);
        }
        catch (OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
        {
            s.Dispose();
            this.status.OnNext(PrinterConnectionState.Disconnected);
            throw new TimeoutException($"Timed out connecting to {this.host}:{this.port}.");
        }
        catch
        {
            s.Dispose();
            this.status.OnNext(PrinterConnectionState.Disconnected);
            throw;
        }

        this.socket = s;
        this.status.OnNext(PrinterConnectionState.Connected);
    }


    public async ValueTask SendAsync(ReadOnlyMemory<byte> data, CancellationToken cancellationToken = default)
    {
        var s = this.GetSocket();
        await this.gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            await Write(s, data, cancellationToken).ConfigureAwait(false);
        }
        finally
        {
            this.gate.Release();
        }
    }


    public async ValueTask<byte[]> Query(ReadOnlyMemory<byte> request, int responseLength, CancellationToken cancellationToken = default)
    {
        ArgumentOutOfRangeException.ThrowIfNegative(responseLength);
        var s = this.GetSocket();

        await this.gate.WaitAsync(cancellationToken).ConfigureAwait(false);
        try
        {
            // a reply to an earlier query that timed out may still be sitting in the buffer
            var scratch = new byte[256];
            while (s.Available > 0)
                await this.Read(s, scratch.AsMemory(0, Math.Min(s.Available, scratch.Length)), cancellationToken).ConfigureAwait(false);

            await Write(s, request, cancellationToken).ConfigureAwait(false);

            var response = new byte[responseLength];
            var read = 0;
            while (read < responseLength)
                read += await this.Read(s, response.AsMemory(read), cancellationToken).ConfigureAwait(false);

            return response;
        }
        finally
        {
            this.gate.Release();
        }
    }


    Socket GetSocket()
    {
        var s = this.socket;
        if (s is null || !s.Connected)
            throw new InvalidOperationException("The printer is not connected.");

        return s;
    }


    async ValueTask<int> Read(Socket s, Memory<byte> buffer, CancellationToken cancellationToken)
    {
        var read = await s.ReceiveAsync(buffer, SocketFlags.None, cancellationToken).ConfigureAwait(false);
        if (read == 0)
        {
            // an orderly close from the printer - the socket is no use for printing either
            this.Disconnect();
            throw new IOException("The printer closed the connection.");
        }
        return read;
    }


    static async ValueTask Write(Socket s, ReadOnlyMemory<byte> data, CancellationToken cancellationToken)
    {
        var remaining = data;
        while (!remaining.IsEmpty)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var sent = await s.SendAsync(remaining, SocketFlags.None, cancellationToken).ConfigureAwait(false);
            if (sent <= 0)
                throw new IOException("The printer socket closed mid-write.");

            remaining = remaining[sent..];
        }
    }


    /// <summary>Closes the socket. Always call this (or <see cref="Dispose"/>) when finished.</summary>
    public void Disconnect()
    {
        var s = this.socket;
        this.socket = null;
        if (s is null)
            return;

        try
        {
            if (s.Connected)
                s.Shutdown(SocketShutdown.Both);
        }
        catch { /* already torn down */ }
        finally
        {
            s.Dispose();
            this.status.OnNext(PrinterConnectionState.Disconnected);
        }
    }


    public void Dispose()
    {
        this.Disconnect();
        this.status.Dispose();
        this.gate.Dispose();
    }
}
