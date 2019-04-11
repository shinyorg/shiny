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

        public Task Cancel(IHttpTransfer transfer = null)
        {
            throw new NotImplementedException();
        }

        public Task Cancel(QueryFilter filter)
        {
            throw new NotImplementedException();
        }

        public Task<IHttpTransfer> Enqueue(HttpTransferRequest request)
        {
            throw new NotImplementedException();
        }

        public Task<IHttpTransfer> GetTransfer(string id)
        {
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IHttpTransfer>> GetTransfers(QueryFilter filter = null)
        {
            throw new NotImplementedException();
        }

        public IObservable<IHttpTransfer> WhenUpdated()
        {
            throw new NotImplementedException();
        }
    }
}
