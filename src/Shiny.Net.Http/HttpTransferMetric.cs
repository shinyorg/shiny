using System;
using System.Collections.Generic;
using System.IO;
using System.Text;

namespace Shiny.Net.Http
{
    public struct HttpTransfer
    {
        public string Identifier { get; }
        public string Uri { get; }
        public FileInfo LocalFile { get; }
        public bool IsUpload { get; }
        public bool UseMeteredConnection { get; }

        public Exception Exception { get; }
        public string RemoteFileName { get; }
        public long FileSize { get; }
        public long BytesTransferred { get; }
        public double PercentComplete { get; }
    }


    public struct HttpTransferMetric
    {
        public HttpTransfer Transfer { get; }
        //public IHttpTransfer Transfer { get; }
        public long BytesPerSecond { get; }
        public TimeSpan EstimatedCompletionTime { get; }
    }
}
