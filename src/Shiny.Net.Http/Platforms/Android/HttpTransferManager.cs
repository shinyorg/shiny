using System;
using System.Linq;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Database;
using Observable = System.Reactive.Linq.Observable;
using Native = Android.App.DownloadManager;
using Shiny.Infrastructure;
using Shiny.Net.Http.Infrastructure;
using System.IO;

namespace Shiny.Net.Http
{
    public class HttpTransferManager : AbstractHttpTransferManager
    {
        readonly IAndroidContext context;
        readonly IRepository repository;
        readonly IDictionary<string, HttpTransfer> transfers;


        public HttpTransferManager(IAndroidContext context, IRepository repository)
        {
            this.transfers = new Dictionary<string, HttpTransfer>();

            this.context = context;
            this.repository = repository;
            //TODO: should I start intent service for receiver?
        }



        //public override Task Cancel(QueryFilter filter = null)
        //{
        //}

        public override Task Cancel(IHttpTransfer transfer)
        {
            var id = long.Parse(transfer.Identifier);
            this.context
                .GetManager()
                .Remove(id);
            return Task.CompletedTask;
        }


        static readonly QueryFilter updateFilter = new QueryFilter();

        IObservable<IHttpTransfer> httpObs;
        public override IObservable<IHttpTransfer> WhenUpdated()
        {
            this.httpObs = this.httpObs ?? Observable
                .Create<IHttpTransfer>(ob =>
                {
                    var lastRun = DateTime.UtcNow;
                    return Observable
                        .Interval(TimeSpan.FromSeconds(1))
                        .Subscribe(_ =>
                        {
                            var transfers = this.GetAll(updateFilter);
                            foreach (var transfer in transfers)
                                if (transfer.LastModified >= lastRun)
                                    ob.OnNext(transfer);

                            lastRun = DateTime.UtcNow;
                        });
                })
                .Publish()
                .RefCount();

            return this.httpObs;
        }


        // TODO:
        protected override Task<IHttpTransfer> CreateDownload(HttpTransferRequest request)
        {
            var dlPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath;
            var path = new FileInfo(Path.Combine(dlPath, request.LocalFile.Name));

            var native = new Native
                .Request(Android.Net.Uri.Parse(request.Uri))
                //.SetDestinationUri(request.LocalFile.ToNativeUri()) // TODO: Need WRITE_EXTERNAL_STORAGE
                .SetDestinationUri(path.ToNativeUri())
                .SetAllowedOverMetered(request.UseMeteredConnection);

            foreach (var header in request.Headers)
                native.AddRequestHeader(header.Key, header.Value);

            var id = this.context.GetManager().Enqueue(native);
            //await this.repository.Set(id.ToString(), request);

            var transfer = new HttpTransfer(request, id.ToString());
            //this.Sub(transfer);

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


        IHttpTransfer ToLib(ICursor cursor)
        {
            HttpTransfer transfer = null;
            var id = cursor.GetLong(cursor.GetColumnIndex(Native.ColumnId)).ToString();

            if (!this.transfers.ContainsKey(id))
            {
                var request = this.RebuildRequest(cursor);
                transfer = new HttpTransfer(request, id.ToString());
                //this.Sub(transfer);
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


        protected override Task<IEnumerable<IHttpTransfer>> GetUploads(QueryFilter filter)
        {
            return Task.FromResult(Enumerable.Empty<IHttpTransfer>());
        }


        protected override Task<IEnumerable<IHttpTransfer>> GetDownloads(QueryFilter filter)
            => Task.FromResult(this.GetAll(filter));
    }
}
