using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Android;
using Shiny.Support.Repositories;

namespace Shiny.Net.Http;


public class HttpTransferManager : IHttpTransferManager
{
    readonly AndroidPlatform platform;
    readonly IRepository repository;


    public HttpTransferManager(
        AndroidPlatform platform,
        IRepository repository
    )
    {
        this.platform = platform;
        this.repository = repository;
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

        // TODO: start foreground if not started
        return transfer;
    }


    public Task Cancel(string identifier)
    {
        // this will trigger over to the job if it is running
        this.repository.Remove<HttpTransfer>(identifier);

        // TODO: stop foreground if no other transfers?
            // TODO: foreground will do it, itself
        return Task.CompletedTask;
    }


    public Task CancelAll()
    {
        // this will trigger over to the job if it is running
        this.repository.Clear<HttpTransfer>();
        return Task.CompletedTask;

        // TODO: stop foreground
        // TODO: foreground will do it, itself
    }


    public IObservable<int> WatchCount() => this.repository.CreateCountWatcher<HttpTransfer>();
    public IObservable<HttpTransferResult> WhenUpdateReceived() => HttpTransferProcess.WhenProgress();
}