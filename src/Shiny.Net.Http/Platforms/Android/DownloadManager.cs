using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Database;
using Shiny.Infrastructure;
using Native = Android.App.DownloadManager;


namespace Shiny.Net.Http
{
    public class DownloadManager : IDownloadManager
    {
        readonly IAndroidContext context;
        readonly IRepository repository;
        readonly object syncLock;
        readonly IDictionary<long, DownloadHttpTransfer> transfers;


        public DownloadManager(IAndroidContext context, IRepository repository)
        {
            this.syncLock = new object();
            this.transfers = new Dictionary<long, DownloadHttpTransfer>();

            this.context = context;
            this.repository = repository;
        }


        public Task Cancel(IHttpTransfer transfer)
        {
            throw new NotImplementedException();
        }


        public Task CancelAll()
        {
            // TODO
            return Task.CompletedTask;
        }


        public Task<IEnumerable<IHttpTransfer>> GetTransfers()
            => Task.FromResult(this.GetAll());


        //public IObservable<Unit> Loop()
        //{
        //    var query = new Native.Query();
        //    query.SetFilterByStatus(
        //        DownloadStatus.Paused |
        //        DownloadStatus.Pending |
        //        DownloadStatus.Running
        //    );

        //    using (var cursor = this.GetManager().InvokeQuery(query))
        //    {
        //        while (cursor.MoveToNext())
        //        {
        //            // pump each var
        //        }
        //        cursor?.Close();
        //    }
        //}

        public async Task<IHttpTransfer> Create(HttpTransferRequest request)
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
            await this.repository.Set(id.ToString(), request);

            var transfer = new DownloadHttpTransfer(request, id);
            lock (this.syncLock)
                this.transfers.Add(id, transfer);

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


        public IHttpTransfer Get(long id)
        {
            var query = new Native.Query();
            query.SetFilterById(id);
            using (var cursor = this.context.GetManager().InvokeQuery(query))
            {
                if (cursor.MoveToFirst())
                {
                    return this.ToLib(cursor);
                }
            }
            return null;
        }


        IHttpTransfer ToLib(ICursor cursor)
        {
            var id = cursor.GetLong(cursor.GetColumnIndex(Native.ColumnId));
            if (!this.transfers.ContainsKey(id))
            {
                lock (this.syncLock)
                {
                    if (!this.transfers.ContainsKey(id))
                    {
                        var request = this.RebuildRequest(cursor);
                        var transfer = new DownloadHttpTransfer(request, id);
                        transfer.Refresh(cursor);
                        this.transfers.Add(id, transfer);
                    }
                }
            }
            return this.transfers[id];
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
