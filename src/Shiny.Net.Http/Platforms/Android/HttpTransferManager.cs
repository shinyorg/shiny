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
    public class HttpTransferManager : IHttpTransferManager
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


        public Task Cancel(IHttpTransfer transfer)
        {
            return Task.CompletedTask;
        }


        public Task CancelAll()
        {
            // TODO
            return Task.CompletedTask;
        }


        public Task<IEnumerable<IHttpTransfer>> GetTransfers()
            => Task.FromResult(this.GetAll());


        public async Task<IHttpTransfer> Enqueue(HttpTransferRequest request)
        {
            var native = new Native.Request(request.LocalFile.ToNativeUri());
            //native.SetAllowedNetworkTypes(DownloadNetwork.Wifi)
            //native.SetAllowedOverRoaming()
            //native.SetNotificationVisibility(DownloadVisibility.Visible);
            //native.SetRequiresDeviceIdle
            //native.SetRequiresCharging
            //native.SetTitle("")
            //native.SetVisibleInDownloadsUi(true);
            native.SetAllowedOverMetered(request.UseMeteredConnection);

            foreach (var header in request.Headers)
                native.AddRequestHeader(header.Key, header.Value);

            var id = this.context.GetManager().Enqueue(native);
            //await this.repository.Set(id.ToString(), request);

            var transfer = new HttpTransfer(this, request, id.ToString());
            this.Sub(transfer);

            return transfer;
        }


        public IEnumerable<IHttpTransfer> GetAll()
        {
            using (var cursor = this.context.GetManager().InvokeQuery(new Native.Query()))
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
            var ids = this.transfers.Keys.Select(long.Parse).ToArray();
            var query = new Native.Query().SetFilterById(ids);

            using (var cursor = this.context.GetManager().InvokeQuery(query))
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
                transfer = new HttpTransfer(this, request, id.ToString());
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

            var request = new HttpTransferRequest(uri, localPath);
            return request;
        }
    }
}
