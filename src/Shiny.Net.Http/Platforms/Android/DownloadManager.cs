using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Android.Content;
using Native = Android.App.DownloadManager;
using Shiny.Net.Http.Internals;


namespace Shiny.Net.Http
{
    public class DownloadManager : IDownloadManager
    {
        readonly IAndroidContext context;
        readonly IRepository repository;


        public DownloadManager(IAndroidContext context, IRepository repository)
        {
            this.context = context;
            this.repository = repository;
            //ar reference = intent.GetLongExtra(Android.App.DownloadManager.ExtraDownloadId, -1);

            //var downloadFile = CrossDownloadManager.Current.Queue.Cast<DownloadFileImplementation>().FirstOrDefault(f => f.Id == reference);
            //if (downloadFile == null) return;

            //var query = new Android.App.DownloadManager.Query();
            //query.SetFilterById(downloadFile.Id);

            //try
            //{
            //    using (var cursor = ((Android.App.DownloadManager)context.GetSystemService(Context.DownloadService)).InvokeQuery(query))
            //    {
            //        while (cursor != null && cursor.MoveToNext())
            //        {
            //            ((DownloadManagerImplementation)CrossDownloadManager.Current).UpdateFileProperties(cursor, downloadFile);
            //        }
            //        cursor?.Close();
            //    }
            //}
            //catch (Android.Database.Sqlite.SQLiteException)
            //{
            //    // I lately got an exception that the database was unaccessible ...
            //}

            //this.GetManager().InvokeQuery()
        }


        public Task CancelAll()
        {
            throw new NotImplementedException();
        }

        public async Task<IHttpTransfer> Create(HttpTransferRequest request)
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

            var id = this.GetManager().Enqueue(native);
            //await this.repository.Set(id.ToString(), new HttpTransferStore
            //{

            //});
            return null;
        }


        public async Task<IEnumerable<IHttpTransfer>> GetTransfers()
        {
            var transfers = await this.repository.GetAll<HttpTransferStore>();
            var query = new Native.Query();

            throw new NotImplementedException();
        }


        Native downloadManager;
        Native GetManager()
        {
            if (this.downloadManager == null || this.downloadManager.Handle == IntPtr.Zero)
                this.downloadManager = (Native)this.context.AppContext.GetSystemService(Context.DownloadService);

            return this.downloadManager;
        }
    }
}
