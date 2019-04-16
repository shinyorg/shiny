using System;


namespace Shiny.Net.Http
{
    public struct HttpTransfer
    {
        public HttpTransfer(string identifier, string uri, string localFilePath, bool isUpload, bool useMeteredConnection, Exception exception, long fileSize, long bytesTransferred, HttpTransferState status)
        {
            this.Identifier = identifier;
            this.Uri = uri;
            this.LocalFilePath = localFilePath;
            this.IsUpload = isUpload;
            this.UseMeteredConnection = useMeteredConnection;
            this.Exception = exception;
            this.FileSize = fileSize;
            this.BytesTransferred = bytesTransferred;

            this.Status = exception == null
                ? status
                : HttpTransferState.Error;
        }


        public string Identifier { get; }
        public string Uri { get; }
        public string LocalFilePath { get; }
        public bool IsUpload { get; }
        public bool UseMeteredConnection { get; }
        public Exception Exception { get; }
        public long FileSize { get; }
        public long BytesTransferred { get; }
        public HttpTransferState Status { get; }
        public double PercentComplete
        {
            get
            {
                if (this.BytesTransferred <= 0 || this.FileSize <= 0)
                    return 0;

                var raw = ((double)this.BytesTransferred / (double)this.FileSize);
                return Math.Round(raw, 2);
            }
        }
    }
}
