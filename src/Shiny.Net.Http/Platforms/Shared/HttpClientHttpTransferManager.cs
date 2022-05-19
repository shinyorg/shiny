using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Stores;

namespace Shiny.Net.Http;


public class HttpClientHttpTransferManager : AbstractHttpTransferManager, IShinyStartupTask
{
    public HttpClientHttpTransferManager(ILogger logger, IRepository<HttpTransfer> repository)
    {
        this.Logger = logger;
        this.Repository = repository;
    }


    protected IRepository<HttpTransfer> Repository { get; }
    protected ILogger Logger { get; }


    protected override Task<IEnumerable<HttpTransfer>> GetDownloads(QueryFilter filter)
        => this.Query(filter, false);


    protected override Task<IEnumerable<HttpTransfer>> GetUploads(QueryFilter filter)
        => this.Query(filter, true);


    async Task<IEnumerable<HttpTransfer>> Query(QueryFilter filter, bool isUpload)
    {
        var stores = await this.Repository
            .GetList()
            .ConfigureAwait(false);

        var query = stores
            .Where(x => x.IsUpload == isUpload);

        if (filter.Ids.Any())
            query = query.Where(x => filter.Ids.Any(y => x.Id == y));

        return query.Select(x => new HttpTransfer(
            x.Id,
            x.Uri,
            x.LocalFile,
            isUpload,
            x.UseMeteredConnection,
            null,
            0L,
            0L,
            HttpTransferState.Pending
        ));
    }


    // TODO: stop foreground service if running
    public override Task Cancel(string id) => Task.WhenAll(
        this.Repository.Remove(id)
    );

    // TODO: start foreground service
    protected override Task<HttpTransfer> CreateDownload(HttpTransferRequest request)
        => this.Create(request);


    protected override Task<HttpTransfer> CreateUpload(HttpTransferRequest request)
        => this.Create(request);


    async Task<HttpTransfer> Create(HttpTransferRequest request)
    {
        var transfer = new HttpTransfer(
            Guid.NewGuid().ToString(),
            request.Uri,
            request.LocalFile.FullName,
            request.IsUpload,
            request.UseMeteredConnection,
            null,
            request.IsUpload ? request.LocalFile.Length : 0L,
            0,
            HttpTransferState.Pending
        //TODO
        //HttpMethod = request.HttpMethod.ToString(),
        //Headers = request.Headers
        );

        await this.Repository
            .Set(transfer)
            .ConfigureAwait(false);


        // TODO: need to create a message to background service through repo or messagebus
        return transfer;
    }


    // overrides will have to merge with the base if they are only overriding one of the directions
    public override IObservable<HttpTransfer> WhenUpdated() => null; // TODO
        //=> this.Services.Bus.Listener<HttpTransfer>();


    public virtual async void Start()
    {
        try
        {
            var requests = await this.Repository
                .GetList()
                .ConfigureAwait(false);

            //foreach (var request in reuests)
            //    await this.jobManager.Run(request.Id).ConfigureAwait(false);
        }
        catch (Exception ex)
        {
            this.Logger.LogError(ex, "Error restarting HTTP transfer manager");
        }
    }
}