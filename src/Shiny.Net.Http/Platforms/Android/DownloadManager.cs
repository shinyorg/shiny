using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Android.Content;
using Native = Android.App.DownloadManager;


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
        }


        public Task CancelAll()
        {
            throw new NotImplementedException();
        }

        public Task<IHttpTransfer> Create(HttpTransferRequest request)
        {
            var native = new Native.Request(null);
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
            return null;
        }

        public Task<IEnumerable<IHttpTransfer>> GetTransfers()
        {
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
