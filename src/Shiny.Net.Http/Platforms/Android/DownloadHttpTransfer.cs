using System;
using Native = Android.App.DownloadManager;


namespace Shiny.Net.Http
{
    public class DownloadHttpTransfer : AbstractHttpTransfer
    {
        public DownloadHttpTransfer(HttpTransferRequest request) : base(request, false) { }


        public void Refresh(long id)
        {
        }

        public override void Cancel()
        {
            throw new NotImplementedException();
        }
    }
}
