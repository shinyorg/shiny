using System;
using Foundation;

namespace Shiny.BluetoothLE;


public class InputStream : System.IO.Stream
{
    readonly NSInputStream stream;

    public InputStream(NSInputStream stream)
        => this.stream = stream;


    public override void Flush()
        => throw new NotSupportedException();
    

    public override int Read(byte[] buffer, int offset, int count)
    {
        if (offset != 0)
            throw new NotSupportedException();

        return (int)this.stream!.Read(buffer, (nuint)count);
    }

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
