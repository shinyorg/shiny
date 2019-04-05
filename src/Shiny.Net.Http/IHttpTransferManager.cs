using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Net.Http
{
    public interface IHttpTransferManager
    {
        //native.SetAllowedNetworkTypes(DownloadNetwork.Wifi)
        //native.SetAllowedOverRoaming()
        //native.SetNotificationVisibility(DownloadVisibility.Visible);
        //native.SetRequiresDeviceIdle
        //native.SetRequiresCharging
        //native.SetTitle("")
        //native.SetDescription()
        //native.SetVisibleInDownloadsUi(true);
        //native.SetShowRunningNotification
        Task<IHttpTransfer> Enqueue(HttpTransferRequest request);
        Task<IEnumerable<IHttpTransfer>> GetTransfers(QueryFilter filter = null);
        Task Cancel(IHttpTransfer transfer = null);
        Task Cancel(QueryFilter filter);
    }
}
