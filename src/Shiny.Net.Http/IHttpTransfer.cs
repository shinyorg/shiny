using System;


namespace Shiny.Net.Http
{
    public interface IHttpTransfer
    {
        HttpTransferRequest Request { get; }
        string Identifier { get; }
        bool IsUpload { get; }
        HttpTransferState Status { get; }
        string RemoteFileName { get; }

        long ResumeOffset { get; }
        long FileSize { get; }
        long BytesTransferred { get; }
        decimal PercentComplete { get; }
        long BytesPerSecond { get; }
        TimeSpan EstimatedCompletionTime { get; }
        DateTimeOffset StartTime { get; }

        Exception Exception { get; }
    }
}