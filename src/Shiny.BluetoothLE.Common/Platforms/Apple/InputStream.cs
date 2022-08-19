using System;
using System.Threading;
using System.Threading.Tasks;
using Foundation;

namespace Shiny.BluetoothLE;


public class InputStream : System.IO.Stream
{
    readonly NSInputStream stream;

    public InputStream(NSInputStream stream)
        => this.stream = stream;


    public override void Flush()
        => throw new NotSupportedException();


    public override async Task<int> ReadAsync(byte[] buffer, int offset, int count, CancellationToken cancellationToken)
    {
        await this.WaitForBytesAvailable(cancellationToken).ConfigureAwait(false);
        var read = (int)this.stream.Read(buffer, offset, (nuint)count);
        return read;
    }


    public async Task WaitForBytesAvailable(CancellationToken cancellationToken)
    {
        if (this.stream.HasBytesAvailable())
            return;

        await this.stream
            .WaitForEvent(
                NSStreamEvent.HasBytesAvailable,
                cancellationToken
            )
            .ConfigureAwait(false);
    }

    
    public override int Read(byte[] buffer, int offset, int count)
        => (int)this.stream!.Read(buffer, offset, (nuint)count);

    public override long Seek(long offset, System.IO.SeekOrigin origin)
        => throw new NotSupportedException();

    public override void SetLength(long value)
        => throw new NotSupportedException();

    public override void Write(byte[] buffer, int offset, int count)
        => throw new NotSupportedException();

    public override bool CanRead => true;
    public override bool CanSeek => false;
    public override bool CanWrite => false;
    public override long Length => throw new NotSupportedException();
    public override long Position
    {
        get => throw new NotSupportedException();
        set => throw new NotSupportedException();
    }
}
