using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Infrastructure;

namespace Shiny.Net.Http
{
    public abstract class AbstractHttpTransferManager : IHttpTransferManager
    {
        protected AbstractHttpTransferManager(IRepository repository)
        {
            this.Repository = repository;
            this.SyncLock = new object();
        }


        protected IRepository Repository { get; }
        protected object SyncLock { get; }
        protected IDictionary<string, IHttpTransfer> CurrentTransfers { get; private set; }

        public abstract Task Cancel(IHttpTransfer transfer);
        public virtual async Task Cancel(QueryFilter filter)
        {
            lock (this.SyncLock)
                this.CurrentTransfers?.Clear();

            await this.Repository.Clear<HttpTransferRequest>();
        }


        public virtual async Task<IEnumerable<IHttpTransfer>> GetTransfers(QueryFilter filter)
        {
            if (this.CurrentTransfers == null)
            {
                lock (this.SyncLock)
                {
                    if (this.CurrentTransfers == null)
                    {
                        this.CurrentTransfers = new Dictionary<string, IHttpTransfer>();
                        //await this.Repository.GetAll<HttpTransferRequest>(); // TODO: need Identifier
                    }
                }
            }
            throw new NotImplementedException();
        }


        public virtual IObservable<IHttpTransfer> WhenChanged()
        {
            throw new NotImplementedException();
        }


        public virtual async Task<IHttpTransfer> Enqueue(HttpTransferRequest request)
        {
            var task = request.IsUpload
                ? this.CreateUpload(request)
                : this.CreateDownload(request);

            var transfer = await task.ConfigureAwait(false);
            return transfer;
        }


        protected virtual Task<IHttpTransfer> CreateUpload(HttpTransferRequest request)
            => Task.FromResult<IHttpTransfer>(new HttpClientHttpTransfer(request, Guid.NewGuid().ToString()));


        protected virtual Task<IHttpTransfer> CreateDownload(HttpTransferRequest request)
            => Task.FromResult<IHttpTransfer>(new HttpClientHttpTransfer(request, Guid.NewGuid().ToString()));
    }
}
