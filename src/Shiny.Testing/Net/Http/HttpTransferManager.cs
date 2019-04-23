using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Net.Http;

namespace Shiny.Testing.Net.Http
{
    public class UploadManager : IHttpTransferManager
    {
        public Task Cancel(string id)
        {
            throw new NotImplementedException();
        }


        public Task Cancel(QueryFilter filter = null)
        {
            throw new NotImplementedException();
        }

        public Task<HttpTransfer> Enqueue(HttpTransferRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<HttpTransfer> GetTransfer(string id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<HttpTransfer>> GetTransfers(QueryFilter filter = null)
        {
            throw new NotImplementedException();
        }

        public IObservable<HttpTransfer> WhenUpdated()
        {
            throw new NotImplementedException();
        }
    }
}
