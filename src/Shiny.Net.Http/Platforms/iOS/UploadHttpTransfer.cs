using System;
using Foundation;


namespace Shiny.Net.Http
{
    public class UploadHttpTransfer : AbstractHttpTransfer
    {
        public NSUrlSessionUploadTask Task { get; }
        public UploadHttpTransfer(HttpTransferRequest request, NSUrlSessionUploadTask task) : base(request)
        {
            this.Task = task;
        }
    }
}
