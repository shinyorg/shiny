using System;


namespace Shiny.Net.Http
{
    class HttpTransfer : IHttpTransfer
    {
        public HttpTransfer(HttpTransferRequest request, string id)
        {
            this.Request = request;
            this.Identifier = id;
            if (request.IsUpload)
            {
                this.FileSize = request.LocalFile.Length;
                this.RemoteFileName = request.LocalFile.Name;
            }
        }


        public HttpTransferRequest Request { get; }
        public string Identifier { get; }
        public bool IsUpload => this.Request.IsUpload;
        public HttpTransferState Status { get; internal set; } = HttpTransferState.Unknown;
        public string RemoteFileName { get; internal set; }
        public long ResumeOffset { get; internal set; }
        public long FileSize { get; internal set; }
        public long BytesTransferred { get; internal set; }
        public long BytesPerSecond { get; internal set; }
        public decimal PercentComplete { get; internal set; }
        public TimeSpan EstimatedCompletionTime { get; internal set; }
        public DateTimeOffset StartTime { get; internal set; }
        public Exception Exception { get; internal set; }
    }
}
