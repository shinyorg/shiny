using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;


namespace Shiny.Net.Http
{
    public abstract class AbstractHttpTransferManager : IHttpTransferManager
    {
        public abstract IObservable<HttpTransfer> WhenUpdated();
        public abstract Task Cancel(string id);


        public virtual async Task Cancel(QueryFilter? filter = null)
        {
            var transfers = await this
                .GetTransfers(filter)
                .ConfigureAwait(false);

            foreach (var transfer in transfers)
                await this.Cancel(transfer.Identifier).ConfigureAwait(false);
        }


        public virtual async Task<HttpTransfer> GetTransfer(string id)
        {
            var transfers = await this.GetTransfers(new QueryFilter(Ids: id)).ConfigureAwait(false);
            return transfers.FirstOrDefault();
        }


        public virtual async Task<IList<HttpTransfer>> GetTransfers(QueryFilter? filter = null)
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
                    //return Enumerable.Concat(t1.Result, t2.Result);
                    // TODO
                    return null;
            }
        }


        public virtual async Task<HttpTransfer> Enqueue(HttpTransferRequest request)
        {
            var task = request.IsUpload
                ? this.CreateUpload(request)
                : this.CreateDownload(request);

            var transfer = await task.ConfigureAwait(false);
            return transfer;
        }


        protected virtual Task<IList<HttpTransfer>> GetUploads(QueryFilter filter)
        {
            throw new NotImplementedException();
        }


        protected virtual Task<IList<HttpTransfer>> GetDownloads(QueryFilter filter)
        {
            throw new NotImplementedException();
        }


        protected virtual Task<HttpTransfer> CreateUpload(HttpTransferRequest request)
        {
            throw new NotImplementedException();
        }


        protected virtual Task<HttpTransfer> CreateDownload(HttpTransferRequest request)
        {
            throw new NotImplementedException();
        }
    }
}
