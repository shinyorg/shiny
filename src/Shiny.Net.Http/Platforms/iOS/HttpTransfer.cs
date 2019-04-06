using System;
using Foundation;


namespace Shiny.Net.Http
{
    public class HttpTransfer : IHttpTransfer
    {
        public HttpTransfer(NSUrlSessionDownloadTask task, HttpTransferRequest request)
        {
            this.Request = request;
            this.DownloadTask = task;
            this.Identifier = task.TaskIdentifier.ToString();
        }


        public HttpTransfer(NSUrlSessionUploadTask task, HttpTransferRequest request)
        {
            this.Request = request;
            this.UploadTask = task;
            this.Identifier = task.TaskIdentifier.ToString();
        }


        public NSUrlSessionDownloadTask DownloadTask { get; }
        public NSUrlSessionUploadTask UploadTask { get; }

        public HttpTransferRequest Request { get; }
        public string Identifier { get; }
        public HttpTransferState Status { get; internal set; }


        Exception exception;
        public Exception Exception
        {
            get => this.exception;
            internal set
            {
                this.Status = HttpTransferState.Error;
                this.exception = value;
            }
        }

        public string RemoteFileName => this.DownloadTask?.Response?.SuggestedFilename;
        public long FileSize { get; internal set; }
        public long BytesTransferred { get; internal set; }
        public DateTime LastModified { get; internal set; }
    }
}