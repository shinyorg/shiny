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
            //this.GetManager().InvokeQuery()
        }


        public Task CancelAll()
        {
            throw new NotImplementedException();
        }

        public async Task<IHttpTransfer> Create(HttpTransferRequest request)
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
            await this.repository.Set(id.ToString(), new HttpTransferStore
            {

            });
            return null;
        }


        public async Task<IEnumerable<IHttpTransfer>> GetTransfers()
        {
            var transfers = await this.repository.GetAll<HttpTransferStore>();

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
