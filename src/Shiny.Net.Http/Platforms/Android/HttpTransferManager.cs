using System;
using System.Linq;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Stores;
using Shiny.Stores.Impl;

namespace Shiny.Net.Http;


class HttpTransferManager : IHttpTransferManager, IShinyStartupTask, IShinyComponentStartup
{
    readonly ILogger logger;
    readonly AndroidPlatform platform;
    readonly IServiceProvider services;
    readonly IConnectivity connectivity;
    readonly IRepository<BlobStore<HttpTransferRequest>> repository;


    public HttpTransferManager(
        ILogger<HttpTransferManager> logger,
        AndroidPlatform platform,
        IServiceProvider services,
        IConnectivity connectivity,
        IRepository<BlobStore<HttpTransferRequest>> repository
    )
    {
        this.logger = logger;
        this.platform = platform;
        this.services = services;
        this.connectivity = connectivity;
        this.repository = repository;
    }


    public void Start() { }
    public void ComponentStart()
    {
        try
        {
            // TODO: danger - moving back to an async GetList will stop this.  We need to force the collection to load BEFORE the job runs which is a problem
            // the job may not see the collection is loaded.  Switching to GetList method also removes need to be thread safe on the collection
            var requestBlobs = this.repository.GetList();
            foreach (var blob in requestBlobs)
            {
                // TODO: anything that was inprogress, should auto resume - must save that state though,
                // so we know manual 'manual pause' from 'paused by network'?  'Paused by network' could just move back to pending/inprogress

                // job will deal with auto restart
                var ht = this.Create(blob.Object, blob.Identifier);
                this.transfers.Add(ht);
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
                AndroidPermissions.PostNotifications,
                33,
                null
            ))
            .ToTask()
            .ConfigureAwait(false);

        var identifier = Guid.NewGuid().ToString();
        var ht = this.Create(request, identifier);

        this.repository.Set(new BlobStore<HttpTransferRequest>
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
        this.repository.Clear();
        return Task.CompletedTask;
    }


    public Task Cancel(string identifier)
    {
        var ht = this.Get(identifier);

        if (ht != null)
        { 
            ht.Cancel();
            this.transfers.Remove(ht);
        }
        this.repository.Remove(identifier);
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


    HttpTransfer Create(HttpTransferRequest request, string identifier) => new HttpTransfer(
        this.services,
        this.connectivity,
        request,
        identifier
    );

    HttpTransfer? Get(string identifier)
        => this.transfers.FirstOrDefault(x => x.Identifier.Equals(identifier)) as HttpTransfer;
}