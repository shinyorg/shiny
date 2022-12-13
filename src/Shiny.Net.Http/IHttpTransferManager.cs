using System;
using System.Threading.Tasks;

namespace Shiny.Net.Http;


public interface IHttpTransferManager
{
    INotifyReadOnlyCollection<IHttpTransfer> Transfers { get; }
    Task<IHttpTransfer> Add(HttpTransferRequest request);
    Task Remove(string identifier);

    //native.SetAllowedNetworkTypes(DownloadNetwork.Wifi)
    //native.SetNotificationVisibility(DownloadVisibility.Visible);
    //native.SetRequiresDeviceIdle
    //native.SetRequiresCharging
    //native.SetTitle("")
    //native.SetDescription()
    //native.SetVisibleInDownloadsUi(true);
    //native.SetShowRunningNotification
    //Task<HttpTransfer> Enqueue(HttpTransferRequest request);
    //Task<HttpTransfer> GetTransfer(string id);
    //Task<IList<HttpTransfer>> GetTransfers(QueryFilter? filter = null);
    //Task Cancel(string id);
    //Task Cancel(QueryFilter? filter = null);
    //IObservable<HttpTransfer> WhenUpdated();
}
