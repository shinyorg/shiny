using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Net.Http
{
    public interface IHttpTransferManager
    {
        //native.SetAllowedNetworkTypes(DownloadNetwork.Wifi)
        //native.SetNotificationVisibility(DownloadVisibility.Visible);
        //native.SetRequiresDeviceIdle
        //native.SetRequiresCharging
        //native.SetTitle("")
        //native.SetDescription()
        //native.SetVisibleInDownloadsUi(true);
        //native.SetShowRunningNotification
        Task<HttpTransfer> Enqueue(HttpTransferRequest request);
        Task<HttpTransfer> GetTransfer(string id);
        Task<IEnumerable<HttpTransfer>> GetTransfers(QueryFilter filter = null);
        Task Cancel(string id);
        Task Cancel(QueryFilter filter);
        IObservable<HttpTransfer> WhenUpdated();
    }
}
