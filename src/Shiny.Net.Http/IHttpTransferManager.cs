using System;
using System.Collections.Generic;
using System.Threading.Tasks;

namespace Shiny.Net.Http;


public interface IHttpTransferManager : IShinyForegroundManager
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
    Task<IList<HttpTransfer>> GetTransfers(QueryFilter? filter = null);
    Task Cancel(string id);
    Task Cancel(QueryFilter? filter = null);
    IObservable<HttpTransfer> WhenUpdated();
}
