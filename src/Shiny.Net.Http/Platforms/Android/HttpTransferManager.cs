using System;
using System.Linq;
using System.Threading.Tasks;

namespace Shiny.Net.Http;


class HttpTransferManager : IHttpTransferManager, IShinyStartupTask
{
    readonly IServiceProvider services;


    public HttpTransferManager(IServiceProvider services)
    {
        this.services = services;
    }


    public void Start()
    {
    }


    readonly ObservableList<IHttpTransfer> transfers = new();
    public INotifyReadOnlyCollection<IHttpTransfer> Transfers => this.transfers;


    public async Task<IHttpTransfer> AddToQueue(HttpTransferRequest request)
    {
        // TODO: persistent notifications with progress
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
        return null;
    }


    public async Task<IHttpTransfer> Queue(HttpTransferRequest request)
    {
        // TODO: save
        var ht = new HttpTransfer(this.services, request, Guid.NewGuid().ToString());
        return ht;
    }


    public Task CancelAll()
    {
        foreach (HttpTransfer transfer in this.Transfers)
            transfer.Cancel();

        this.transfers.Clear();
        return Task.CompletedTask;
    }


    public Task Cancel(string identifier)
    {
        this.Get(identifier)?.Cancel();
        return Task.CompletedTask;
    }


    public Task Pause(string identifier)
    {
        this.Get(identifier)?.Pause();
        return Task.CompletedTask;
    }


    public async Task Resume(string identifier)
    {
        var transfer = this.Get(identifier);
        if (transfer != null)
            await transfer.Resume().ConfigureAwait(false);
    }


    HttpTransfer? Get(string identifier)
           => this.transfers.FirstOrDefault(x => x.Identifier.Equals(identifier)) as HttpTransfer;
}