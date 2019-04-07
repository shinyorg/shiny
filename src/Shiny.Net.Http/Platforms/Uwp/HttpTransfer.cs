using System;
using Windows.Networking.BackgroundTransfer;
using Shiny.Net.Http.Infrastructure;


namespace Shiny.Net.Http
{
    class HttpTransfer : AbstractHttpTransfer
    {
        public HttpTransfer(HttpTransferRequest request, DownloadOperation operation) : base(request, "TODO")
        {

        }


        public HttpTransfer(HttpTransferRequest request, UploadOperation operation) : base(request, "TODO")
        {

        }
    }
}
