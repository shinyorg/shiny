using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Net.Http
{
    public class HttpTransferManager : AbstractHttpTransferManager
    {
        public override Task Cancel(IHttpTransfer transfer)
        {
            throw new NotImplementedException();
        }

        public override IObservable<IHttpTransfer> WhenUpdated()
        {
            throw new NotImplementedException();
        }

        protected override Task<IEnumerable<IHttpTransfer>> GetDownloads(QueryFilter filter)
        {
            throw new NotImplementedException();
        }

        protected override Task<IEnumerable<IHttpTransfer>> GetUploads(QueryFilter filter)
        {
            throw new NotImplementedException();
        }
    }
}
