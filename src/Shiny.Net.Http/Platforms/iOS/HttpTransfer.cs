using System;
using Foundation;


namespace Shiny.Net.Http
{
    class HttpTransfer : AbstractHttpTransfer
    {
        public NSUrlSessionDownloadTask DownloadTask { get; }
        public NSUrlSessionUploadTask UploadTask { get; }

        public HttpTransfer(HttpTransferRequest request, NSUrlSessionDownloadTask task) : base(request)
        {
            this.DownloadTask = task;
        }


        public HttpTransfer(HttpTransferRequest request, NSUrlSessionUploadTask task) : base(request)
        {
            this.UploadTask = task;
        }
    }
}