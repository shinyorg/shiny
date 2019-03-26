using System;


namespace Shiny.Net.Http
{
    public class DownloadHttpTransfer : AbstractHttpTransfer
    {
        public DownloadHttpTransfer(HttpTransferRequest request) : base(request, false) { }


        public override void Cancel()
        {
            throw new NotImplementedException();
        }
    }
}
