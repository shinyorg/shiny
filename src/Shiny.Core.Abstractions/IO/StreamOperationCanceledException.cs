using System;


namespace Shiny.IO
{
    public class StreamOperationCanceledException : Exception
    {
        public StreamOperationCanceledException() : base("Stream operations have been cancelled") { }
    }
}
