using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.Content;
using Native = Android.App.DownloadManager;
using Shiny.Infrastructure;
using Shiny.Net.Http.Infrastructure;


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

            //await this.repository.Set(id.ToString(), new HttpTransferStore
            //{

            //});
            return null;
        }


        public async Task<IEnumerable<IHttpTransfer>> GetTransfers()
        {
            //var transfers = await this.repository.GetAll<HttpTransferStore>();
            return null;
        }
    }
}
