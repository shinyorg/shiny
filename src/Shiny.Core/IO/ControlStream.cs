using System;
using System.IO;


namespace Shiny.IO
{
    public class ControlStream : Stream
    {
        readonly Stream innerStream;


        public ControlStream(Stream innerStream)
        {
            if (innerStream == null)
                throw new NullReferenceException("innerStream");

            this.innerStream = innerStream;
        }


        public bool IsOperationsCancelled { get; private set; }
        public virtual void CancelOperations() => this.IsOperationsCancelled = true;


        public override int Read(byte[] buffer, int offset, int count)
        {
            if (this.IsOperationsCancelled)
                throw new StreamOperationCanceledException();

            var read = this.innerStream.Read(buffer, offset, count);
            this.OnEvent(true, this.Position, this.Length, read);
            return read;
        }


        public override void Write(byte[] buffer, int offset, int count)
        {
            if (this.IsOperationsCancelled)
                throw new StreamOperationCanceledException();

            this.innerStream.Write(buffer, offset, count);
            this.OnEvent(false, this.Position, this.Length, count);
        }


        protected virtual void OnEvent(bool read, long position, long length, int byteCount)
        {
            var e = new ControlStreamEventArgs(read, byteCount, length, byteCount);
            this.BytesMoved?.Invoke(this, e);

            if (read)
                this.BytesRead?.Invoke(this, e);
            else
                this.BytesWritten?.Invoke(this, e);
        }


        public override long Seek(long offset, SeekOrigin origin) => this.innerStream.Seek(offset, origin);
        public override void SetLength(long value) => this.innerStream.SetLength(value);
        public override void Flush() => this.innerStream.Flush();

        public event EventHandler<ControlStreamEventArgs>? BytesMoved;
        public event EventHandler<ControlStreamEventArgs>? BytesRead;
        public event EventHandler<ControlStreamEventArgs>? BytesWritten;

        public override int ReadTimeout => this.innerStream.ReadTimeout;
        public override int WriteTimeout => this.innerStream.WriteTimeout;
        public override bool CanRead => this.innerStream.CanRead;
        public override bool CanSeek => this.innerStream.CanSeek;
        public override bool CanWrite => this.innerStream.CanWrite;
        public override long Length => this.innerStream.Length;
        public override long Position
        {
            get => this.innerStream.Position;
            set => this.innerStream.Position = value;
        }
    }
}