using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android;
using Microsoft.Extensions.Logging;
using Shiny.Support.Repositories;

namespace Shiny.Net.Http;


public class HttpTransferManager : IHttpTransferManager, IShinyStartupTask
{
    readonly AndroidPlatform platform;
    readonly ILogger logger;
    readonly IRepository repository;


    public HttpTransferManager(
        AndroidPlatform platform,
        ILogger<HttpTransferManager> logger,
        IRepository repository
    )
    {
        this.platform = platform;
        this.logger = logger;
        this.repository = repository;
    }


    public void Start()
    {
        try
        {
            if (HttpTransferService.IsStarted)
                return;

            var transfers = this.repository.GetList<HttpTransfer>();
            if (transfers.Count > 0)
                this.platform.StartService(typeof(HttpTransferService));
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Failed to auto-start HTTP Transfer Manager");
        }
    }


    public Task<IList<HttpTransfer>> GetTransfers()
    {
        var transfers = this.repository.GetList<HttpTransfer>();
        return Task.FromResult(transfers);
    }


    public async Task<HttpTransfer> Queue(HttpTransferRequest request)
    {
        (await this.platform.RequestAccess(Manifest.Permission.ForegroundService)).Assert();
        request.AssertValid();

        // this will trigger over to the job if it is running
        long? contentLength = null;
        if (request.IsUpload)
            contentLength = new FileInfo(request.LocalFilePath).Length;

        var transfer = new HttpTransfer(
            request,
            contentLength,
            0,
            HttpTransferState.Pending,
            DateTimeOffset.UtcNow
        );
        this.repository.Insert(transfer);

        if (!HttpTransferService.IsStarted)
            this.platform.StartService(typeof(HttpTransferService));

        return transfer;
    }


    public Task Cancel(string identifier)
    {
        // this will trigger over to the foreground service which will shut itself down if there are no other transfers
        this.repository.Remove<HttpTransfer>(identifier);
        return Task.CompletedTask;
    }


    public Task CancelAll()
    {
        // this will trigger over to the foreground service which will shut itself down
        this.repository.Clear<HttpTransfer>();
        return Task.CompletedTask;
    }


    public IObservable<int> WatchCount() => this.repository.CreateCountWatcher<HttpTransfer>();
    public IObservable<HttpTransferResult> WhenUpdateReceived() => HttpTransferProcess.WhenProgress();
}