using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Shiny.Net.Http
{
    public abstract class AbstractHttpTransferManager : IHttpTransferManager
    {
        public abstract Task Cancel(IHttpTransfer transfer);
        public abstract IObservable<IHttpTransfer> WhenUpdated();


        public virtual async Task Cancel(string id)
        {
            var task = await this.GetTransfer(id).ConfigureAwait(false);
            if (task != null)
                await this.Cancel(task).ConfigureAwait(false);
        }


        public virtual async Task Cancel(QueryFilter filter = null)
        {
            var transfers = await this
                .GetTransfers(filter)
                .ConfigureAwait(false);

            foreach (var transfer in transfers)
                await this.Cancel(transfer).ConfigureAwait(false);
        }


        public virtual async Task<IHttpTransfer> GetTransfer(string id)
        {
            var transfers = await this.GetTransfers(new QueryFilter().Add(id)).ConfigureAwait(false);
            return transfers.FirstOrDefault();
        }


        public virtual async Task<IEnumerable<IHttpTransfer>> GetTransfers(QueryFilter filter = null)
        {
            filter = filter ?? new QueryFilter();
            switch (filter.Direction)
            {
                case DirectionFilter.Download:
                    return await this.GetDownloads(filter).ConfigureAwait(false);

                case DirectionFilter.Upload:
                    return await this.GetUploads(filter).ConfigureAwait(false);

                default:
                    var t1 = this.GetDownloads(filter);
                    var t2 = this.GetUploads(filter);
                    await Task.WhenAll(t1, t2).ConfigureAwait(false);
                    return Enumerable.Concat(t1.Result, t2.Result);
            }
        }


        public virtual async Task<IHttpTransfer> Enqueue(HttpTransferRequest request)
        {
            var task = request.IsUpload
                ? this.CreateUpload(request)
                : this.CreateDownload(request);

            var transfer = await task.ConfigureAwait(false);
            return transfer;
        }


        protected virtual Task<IEnumerable<IHttpTransfer>> GetUploads(QueryFilter filter)
        {
            throw new NotImplementedException();
        }


        protected virtual Task<IEnumerable<IHttpTransfer>> GetDownloads(QueryFilter filter)
        {
            throw new NotImplementedException();
        }

        protected virtual Task<IHttpTransfer> CreateUpload(HttpTransferRequest request)
            => Task.FromResult<IHttpTransfer>(new HttpClientHttpTransfer(request, Guid.NewGuid().ToString()));


        protected virtual Task<IHttpTransfer> CreateDownload(HttpTransferRequest request)
            => Task.FromResult<IHttpTransfer>(new HttpClientHttpTransfer(request, Guid.NewGuid().ToString()));
    }
}
