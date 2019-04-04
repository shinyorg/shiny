using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny.Net.Http
{
    public class HttpTransferManager : IHttpTransferManager
    {
        //readonly object syncLock;
        readonly IRepository repository;


        public HttpTransferManager(IRepository repository)
        {
            //this.syncLock = new object();
            this.repository = repository;
        }


        public Task Cancel(IHttpTransfer transfer)
        {
            return Task.CompletedTask;
        }


        public Task Cancel(QueryFilter filter = null)
        {
            //lock (this.syncLock)
            //    foreach (var transfer in transfers)
            //        transfer.Value.Cancel();

            return Task.CompletedTask;
        }


        public Task<IHttpTransfer> Enqueue(HttpTransferRequest request)
        {
            //var id = Guid.NewGuid().ToString();
            //await this.repository.Set(id, request);
            //var transfer = new DownloadHttpTransfer(request, id);
            //lock (this.syncLock)
            //    this.transfers.Add(id, transfer);

            //return transfer;
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IHttpTransfer>> GetTransfers(QueryFilter filter = null)
        {
            throw new NotImplementedException();
        }
    }
}
