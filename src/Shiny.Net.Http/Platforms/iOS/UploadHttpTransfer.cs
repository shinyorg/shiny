using System;
using Foundation;

namespace Shiny.Net.Http
{
    public class UploadHttpTransfer : AbstractHttpTransfer
    {
        readonly NSUrlSessionUploadTask task;


        public UploadHttpTransfer(HttpTransferRequest request, NSUrlSessionUploadTask task) : base(request, true)
        {
            this.task = task;
        }


        public override void Cancel()
        {
            throw new NotImplementedException();
        }
    }
}
