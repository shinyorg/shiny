using System;


namespace Shiny.Net.Http
{
    public struct HttpTransferMetric
    {
        public HttpTransferMetric(HttpTransfer transfer, long bytesPerSecond, TimeSpan estimatedTimeRemaining)
        {
            this.Transfer = transfer;
            this.BytesPerSecond = bytesPerSecond;
            this.EstimatedTimeRemaining = estimatedTimeRemaining;
        }


        public HttpTransfer Transfer { get; }
        public long BytesPerSecond { get; }
        public TimeSpan EstimatedTimeRemaining { get; }
    }
}
