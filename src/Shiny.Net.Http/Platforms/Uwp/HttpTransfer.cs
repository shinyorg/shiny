using System;
using Windows.Networking.BackgroundTransfer;

namespace Shiny.Net.Http
{
    class HttpTransfer : AbstractHttpTransfer
    {
        public HttpTransfer(HttpTransferRequest request, DownloadOperation operation) : base(request)
        {

        }


        public HttpTransfer(HttpTransferRequest request, UploadOperation operation) : base(request)
        {

        }
    }
}
