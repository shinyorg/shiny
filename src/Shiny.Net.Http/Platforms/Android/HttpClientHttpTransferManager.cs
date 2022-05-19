using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Stores;

namespace Shiny.Net.Http;


public class HttpClientHttpTransferManager : AbstractHttpTransferManager, IShinyStartupTask
{
    public HttpClientHttpTransferManager(ILogger logger, IRepository repository)
    {
        this.Logger = logger;
        this.Repository = repository;
    }


    protected IRepository Repository { get; }
    protected ILogger Logger { get; }


    protected override Task<IEnumerable<HttpTransfer>> GetDownloads(QueryFilter filter)
        => this.Query(filter, false);


    protected override Task<IEnumerable<HttpTransfer>> GetUploads(QueryFilter filter)
        => this.Query(filter, true);


    async Task<IEnumerable<HttpTransfer>> Query(QueryFilter filter, bool isUpload)
    {
        var stores = await this.Repository
            .GetList<HttpTransferStore>()
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
        this.Repository.Remove<HttpTransferStore>(id)
    );

    // TODO: start foreground service
    protected override Task<HttpTransfer> CreateDownload(HttpTransferRequest request)
        => this.Create(request);


    protected override Task<HttpTransfer> CreateUpload(HttpTransferRequest request)
        => this.Create(request);


    async Task<HttpTransfer> Create(HttpTransferRequest request)
    {
        var id = Guid.NewGuid().ToString();
        await this.Repository
            .Set(id, new HttpTransferStore
            {
                Id = id,
                Uri = request.Uri,
                IsUpload = request.IsUpload,
                PostData = request.PostData,
                LocalFile = request.LocalFile.FullName,
                UseMeteredConnection = request.UseMeteredConnection,
                HttpMethod = request.HttpMethod.ToString(),
                Headers = request.Headers
            })
            .ConfigureAwait(false);

        // TODO: replace with foreground transfer service
        //await this.jobManager.Register(new JobInfo(typeof(TransferJob), id)
        //{
        //    RequiredInternetAccess = InternetAccess.Any,
        //    Repeat = true
        //});
        var transfer = new HttpTransfer(
            id,
            request.Uri,
            request.LocalFile.FullName,
            request.IsUpload,
            request.UseMeteredConnection,
            null,
            request.IsUpload ? request.LocalFile.Length : 0L,
            0,
            HttpTransferState.Pending
        );
        //// fire and forget
        //this.jobManager.Run(id);
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
                .GetList<HttpTransferStore>()
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