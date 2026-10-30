namespace Shiny.Bluetooth.Infrastructure;


/// <summary>
/// Presents a separate input and output stream - Android's <c>BluetoothSocket</c> and WinRT's
/// <c>StreamSocket</c> both hand them out apart - as one bidirectional <see cref="Stream"/>.
/// </summary>
internal sealed class DuplexStream(Stream input, Stream output, Action onClose) : Stream
{
    int closed;

    public override bool CanRead => true;
    public override bool CanWrite => true;
    public override bool CanSeek => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }

    public override int Read(byte[] buffer, int offset, int count) => input.Read(buffer, offset, count);
    public override int Read(Span<byte> buffer) => input.Read(buffer);
    public override Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken ct) => input.ReadAsync(buffer, offset, count, ct);
    public override ValueTask<int> ReadAsync(Memory<byte> buffer, CancellationToken ct = default) => input.ReadAsync(buffer, ct);

    public override void Write(byte[] buffer, int offset, int count) => output.Write(buffer, offset, count);
    public override void Write(ReadOnlySpan<byte> buffer) => output.Write(buffer);
    public override Task WriteAsync(byte[] buffer, int offset, int count, CancellationToken ct) => output.WriteAsync(buffer, offset, count, ct);
    public override ValueTask WriteAsync(ReadOnlyMemory<byte> buffer, CancellationToken ct = default) => output.WriteAsync(buffer, ct);

    public override void Flush() => output.Flush();
    public override Task FlushAsync(CancellationToken ct) => output.FlushAsync(ct);

    public override long Seek(long offset, SeekOrigin origin) => throw new NotSupportedException();
    public override void SetLength(long value) => throw new NotSupportedException();


    protected override void Dispose(bool disposing)
    {
        if (disposing && Interlocked.Exchange(ref this.closed, 1) == 0)
        {
            // the socket goes first - closing it is what unblocks a read pending on the input stream
            onClose();
            input.Dispose();
            output.Dispose();
        }
        base.Dispose(disposing);
    }
}
