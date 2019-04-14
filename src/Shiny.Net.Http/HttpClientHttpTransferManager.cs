using System;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Net.Http.Infrastructure;


namespace Shiny.Net.Http
{
    public class HttpClientHttpTransferManager : AbstractHttpTransferManager
    {
        public HttpClientHttpTransferManager(IRepository repository)
        {
        }


        public override Task Cancel(string id)
        {
            throw new NotImplementedException();
        }


        protected override Task<HttpTransfer> CreateDownload(HttpTransferRequest request)
        {
            return base.CreateDownload(request);
        }


        protected override Task<HttpTransfer> CreateUpload(HttpTransferRequest request)
        {
            return base.CreateUpload(request);
        }


        public override IObservable<HttpTransfer> WhenUpdated()
        {
            throw new NotImplementedException();
        }
    }
}
