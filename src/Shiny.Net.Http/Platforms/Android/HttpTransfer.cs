using System;
using Android.Database;

namespace Shiny.Net.Http
{
    class HttpTransfer : AbstractHttpTransfer
    {
        readonly HttpTransferManager manager;


        public HttpTransfer(HttpTransferManager manager, HttpTransferRequest request, string id) : base(request)
        {
            this.manager = manager;
            this.Identifier = id;
        }


        internal void Refresh(ICursor cursor)
        {

        }
    }
}
