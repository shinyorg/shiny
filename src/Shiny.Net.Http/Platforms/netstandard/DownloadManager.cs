using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Infrastructure;


namespace Shiny.Net.Http
{
    public class DownloadManager : IDownloadManager
    {
        readonly object syncLock;
        readonly IDictionary<string, DownloadHttpTransfer> transfers;
        readonly IRepository repository;


        public DownloadManager(IRepository repository)
        {
            this.syncLock = new object();
            this.repository = repository;
        }


        public Task Cancel(IHttpTransfer transfer)
        {
            return Task.CompletedTask;
        }


        public Task CancelAll()
        {
            //lock (this.syncLock)
            //    foreach (var transfer in transfers)
            //        transfer.Value.Cancel();

            return Task.CompletedTask;
        }


        public async Task<IHttpTransfer> Create(HttpTransferRequest request)
        {
            var id = Guid.NewGuid().ToString();
            await this.repository.Set(id, request);
            var transfer = new DownloadHttpTransfer(request, id);
            lock (this.syncLock)
                this.transfers.Add(id, transfer);

            return transfer;
        }


        public Task<IEnumerable<IHttpTransfer>> GetTransfers()
            => Task.FromResult<IEnumerable<IHttpTransfer>>(this.transfers.Values);
    }
}
