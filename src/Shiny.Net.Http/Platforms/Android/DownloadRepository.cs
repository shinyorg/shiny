using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Database;
using Native = Android.App.DownloadManager;


namespace Shiny.Net.Http
{
    public class DownloadRepository
    {
        readonly IAndroidContext context;
        readonly object syncLock;
        readonly IDictionary<long, DownloadHttpTransfer> transfers;


        public DownloadRepository(IAndroidContext context)
        {
            this.syncLock = new object();
            this.transfers = new Dictionary<long, DownloadHttpTransfer>();
            this.context = context;
        }


        Native downloadManager;
        public Native GetManager()
        {
            if (this.downloadManager == null || this.downloadManager.Handle == IntPtr.Zero)
                this.downloadManager = (Native)this.context.AppContext.GetSystemService(Context.DownloadService);

            return this.downloadManager;
        }


        public IHttpTransfer Create(HttpTransferRequest request)
        {
            var native = new Native.Request(request.LocalFilePath.ToNativeUri());
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

            //await this.repository.Set(id.ToString(), new HttpTransferStore
            //{

            //});
            var id = this.GetManager().Enqueue(native);
            return null;
        }


        public IEnumerable<IHttpTransfer> GetAll()
        {
            using (var cursor = this.GetManager().InvokeQuery(new Native.Query()))
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
            using (var cursor = this.GetManager().InvokeQuery(query))
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
                        var transfer = this.FromCursor(cursor);
                        this.transfers.Add(id, transfer);
                    }
                }
            }
            return this.transfers[id];
        }


        DownloadHttpTransfer FromCursor(ICursor cursor)
        {
            // TODO: missing start date

            //var id = this.GetManager().Enqueue(native);
            var id = cursor.GetLong(cursor.GetColumnIndex(Native.ColumnId));
            var localPath = cursor.GetString(cursor.GetColumnIndex(Native.ColumnLocalUri));
            var totalSize = cursor.GetLong(cursor.GetColumnIndex(Native.ColumnTotalSizeBytes));
            var uri = cursor.GetString(cursor.GetColumnIndex(Native.ColumnUri));
            var bytesDl = cursor.GetInt(cursor.GetColumnIndex(Native.ColumnBytesDownloadedSoFar));

            //var reason = cursor.GetString(cursor.GetColumnIndex(Native.ColumnReason));
            //var lastMod = cursor.GetString(cursor.GetColumnIndex(Native.ColumnLastModifiedTimestamp));
            var status = (DownloadStatus)cursor.GetInt(cursor.GetColumnIndex(Native.ColumnStatus));

            switch (status)
            {
                case DownloadStatus.Failed:
                    break;

                case DownloadStatus.Paused:
                    //Native.ColumnReason
                    //Native.PausedQueuedForWifi
                    //Native.PausedUnknown
                    break;

                case DownloadStatus.Pending:
                    break;

                case DownloadStatus.Running:
                    break;

                case DownloadStatus.Successful:
                    break;
            }
            return null;
        }
    }
}
