using System;
using Foundation;
using Shiny.Net.Http.Infrastructure;


namespace Shiny.Net.Http
{
    public class HttpTransfer : AbstractHttpTransfer
    {
        public HttpTransfer(NSUrlSessionDownloadTask task, HttpTransferRequest request) : base(request, task.TaskIdentifier.ToString())
        {
            this.DownloadTask = task;
            this.NativeIdentifier = task.TaskIdentifier;
        }


        public HttpTransfer(NSUrlSessionUploadTask task, HttpTransferRequest request) : base(request, task.TaskIdentifier.ToString())
        {
            this.UploadTask = task;
            this.NativeIdentifier = task.TaskIdentifier;
            //this.DownloadTask?.Response?.SuggestedFilename;
        }


        public NSUrlSessionDownloadTask DownloadTask { get; }
        public NSUrlSessionUploadTask UploadTask { get; }
        public nuint NativeIdentifier { get; }
    }
}