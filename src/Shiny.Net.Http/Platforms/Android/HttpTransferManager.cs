using System;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Stores;
using Shiny.Stores.Impl;

namespace Shiny.Net.Http;


class HttpTransferManager : IHttpTransferManager, IShinyStartupTask
{
    readonly ILogger logger;
    readonly AndroidPlatform platform;
    readonly IServiceProvider services;
    readonly IRepository<BlobStore<HttpTransferRequest>> repository;


    public HttpTransferManager(
        ILogger<HttpTransferManager> logger,
        AndroidPlatform platform,
        IServiceProvider services,
        IRepository<BlobStore<HttpTransferRequest>> repository
    )
    {
        this.logger = logger;
        this.platform = platform;
        this.services = services;
        this.repository = repository;
    }


    public async void Start()
    {
        try
        {
            var requestBlobs = await this.repository.GetList();
            foreach (var blob in requestBlobs)
            {
                var ht = new HttpTransfer(this.services, blob.Object, blob.Identifier);
                this.transfers.Add(ht);

                // TODO: anything that was inprogress, should auto resume - must save that state
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Could not restart HTTP Transfers");
        }
    }


    readonly ObservableList<IHttpTransfer> transfers = new();
    public INotifyReadOnlyCollection<IHttpTransfer> Transfers => this.transfers;


    public async Task<IHttpTransfer> Queue(HttpTransferRequest request)
    {
        await this.platform
            .RequestFilteredPermissions(new AndroidPermission(
                Android.Manifest.Permission.PostNotifications,
                33,
                null
            ))
            .ToTask();

        var identifier = Guid.NewGuid().ToString();
        var ht = new HttpTransfer(this.services, request, identifier);
        await this.repository.Set(new BlobStore<HttpTransferRequest>
        (
            identifier,
            request
        ));
        this.transfers.Add(ht);

        return ht;
    }


    public Task CancelAll()
    {
        foreach (HttpTransfer transfer in this.Transfers)
            transfer.Cancel();        

        this.transfers.Clear();
        return this.repository.Clear();
    }


    public Task Cancel(string identifier)
    {
        var ht = this.Get(identifier);

        if (ht != null)
        { 
            ht.Cancel();
            this.transfers.Remove(ht);
        }
        return this.repository.Remove(identifier);
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