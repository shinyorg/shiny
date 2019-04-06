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
        Task<IHttpTransfer> Enqueue(HttpTransferRequest request);
        Task<IHttpTransfer> GetTransfer(string id);
        Task<IEnumerable<IHttpTransfer>> GetTransfers(QueryFilter filter = null);
        Task Cancel(string id);
        Task Cancel(IHttpTransfer transfer = null);
        //Task<int> PurgeDatabase(QueryFilter query = null);
        Task Cancel(QueryFilter filter);
        IObservable<IHttpTransfer> WhenUpdated();
    }
}
