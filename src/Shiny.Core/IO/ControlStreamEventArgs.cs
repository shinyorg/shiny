using System;


namespace Shiny.IO
{
    public class ControlStreamEventArgs : EventArgs
    {
        public ControlStreamEventArgs(bool read, long bytes, long length, long position)
        {
            this.IsRead = read;
            this.Bytes = bytes;
            this.Length = length;
            this.Position = position;
        }


        public long Bytes { get; }
        public long Position { get; }
        public long Length { get; }
        public bool IsRead { get; }
    }
}
