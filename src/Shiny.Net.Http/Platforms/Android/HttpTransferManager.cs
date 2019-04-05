using System;
using System.Linq;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Database;
using Shiny.Infrastructure;
using Observable = System.Reactive.Linq.Observable;
using Native = Android.App.DownloadManager;


namespace Shiny.Net.Http
{
    public class HttpTransferManager : AbstractHttpTransferManager
    {
        readonly IAndroidContext context;
        readonly IRepository repository;
        readonly object syncLock;
        readonly IDictionary<string, HttpTransfer> transfers;
        IDisposable refreshSub;


        public HttpTransferManager(IAndroidContext context, IRepository repository)
        {
            this.syncLock = new object();
            this.transfers = new Dictionary<string, HttpTransfer>();

            this.context = context;
            this.repository = repository;
        }


        protected override Task<IHttpTransfer> CreateDownload(HttpTransferRequest request)
        {
            var native = new Native
                          .Request(Android.Net.Uri.Parse(request.Uri))
                          .SetDestinationUri(request.LocalFile.ToNativeUri())
                          .SetAllowedOverMetered(request.UseMeteredConnection);

            foreach (var header in request.Headers)
                native.AddRequestHeader(header.Key, header.Value);

            var id = this.context.GetManager().Enqueue(native);
            //await this.repository.Set(id.ToString(), request);

            var transfer = new HttpTransfer(request, id.ToString());
            this.Sub(transfer);

            return Task.FromResult<IHttpTransfer>(transfer);
        }


        public IEnumerable<IHttpTransfer> GetAll(QueryFilter filter)
        {
            var query = filter.ToNative();
            using (var cursor = this.context.GetManager().InvokeQuery(query))
            {
                while (cursor.MoveToNext())
                {
                    yield return ToLib(cursor);
                }
            }
        }


        void Sub(HttpTransfer transfer)
        {
            lock (this.syncLock)
            {
                this.transfers.Add(transfer.Identifier, transfer);

                // TODO: uploads too
                if (this.refreshSub == null)
                {
                    this.refreshSub = Observable
                        .Interval(TimeSpan.FromSeconds(1))
                        .Subscribe(_ => this.Loop());
                }
            }
        }


        void Loop()
        {
            var filter = new QueryFilter
            {
                Ids = this.transfers.Keys.ToList()
            };

            using (var cursor = this.context.GetManager().InvokeQuery(filter.ToNative()))
            {
                while (cursor.MoveToNext())
                {
                    var t = this.ToLib(cursor);
                    switch (t.Status)
                    {
                        case HttpTransferState.Error:
                        case HttpTransferState.Cancelled:
                        case HttpTransferState.Completed:
                            lock (this.syncLock)
                                this.transfers.Remove(t.Identifier);
                            break;
                    }
                }
            }
        }


        IHttpTransfer ToLib(ICursor cursor)
        {
            HttpTransfer transfer = null;
            var id = cursor.GetLong(cursor.GetColumnIndex(Native.ColumnId)).ToString();

            if (!this.transfers.ContainsKey(id))
            {
                var request = this.RebuildRequest(cursor);
                transfer = new HttpTransfer(request, id.ToString());
                this.Sub(transfer);
            }
            transfer = this.transfers[id];
            transfer.Refresh(cursor);
            return transfer;
        }


        HttpTransferRequest RebuildRequest(ICursor cursor)
        {
            // TODO: can't rebuild enough of the request, will have to swap to repo
            var uri = cursor.GetString(cursor.GetColumnIndex(Native.ColumnUri));
            var localPath = cursor.GetString(cursor.GetColumnIndex(Native.ColumnLocalUri));

            // TODO: unmetered, post data, headers
            var request = new HttpTransferRequest(uri, localPath);
            return request;
        }

        public override Task Cancel(IHttpTransfer transfer)
        {
            throw new NotImplementedException();
        }

        public override IObservable<IHttpTransfer> WhenUpdated()
        {
            throw new NotImplementedException();
        }

        protected override Task<IEnumerable<IHttpTransfer>> GetUploads(QueryFilter filter)
        {
            throw new NotImplementedException();
        }

        protected override Task<IEnumerable<IHttpTransfer>> GetDownloads(QueryFilter filter)
            => Task.FromResult(this.GetAll(filter));
    }
}
