using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Disposables;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Threading.Tasks;
using System.Threading.Tasks;
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
                this.TryStartService();
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
        request.AssertValid();
        (await this.platform.RequestForegroundServicePermissions()).Assert(allowRestricted: true);
        if (OperatingSystemShim.IsAndroidVersionAtLeast(34))
        {
            (await this.platform.RequestAccess("android.permission.FOREGROUND_SERVICE_DATA_SYNC").ToTask()).Assert();
        }
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
        this.TryStartService();

        return transfer;
    }


    public Task Cancel(string identifier)
    {
        // this will trigger over to the foreground service which will shut itself down if there are no other transfers
        var transfer = this.repository.Get<HttpTransfer>(identifier);
        if (transfer != null)
        {
            this.repository.Remove(transfer);

            this.resultSubj.OnNext(new(
                transfer.Request,
                HttpTransferState.Canceled,
                TransferProgress.Empty,
                null
            ));
        }
        return Task.CompletedTask;
    }


    public Task CancelAll()
    {
        // this will trigger over to the foreground service which will shut itself down
        this.repository.Clear<HttpTransfer>();
        return Task.CompletedTask;
    }


    public IObservable<int> WatchCount() => this.repository.CreateCountWatcher<HttpTransfer>();

    readonly Subject<HttpTransferResult> resultSubj = new();
    public IObservable<HttpTransferResult> WhenUpdateReceived() => Observable.Create<HttpTransferResult>(ob =>
    {
        var disposer = new CompositeDisposable();
        this.resultSubj
            .Subscribe(ob.OnNext)
            .DisposedBy(disposer);

        HttpTransferProcess
            .WhenProgress()
            .Subscribe(ob.OnNext)
            .DisposedBy(disposer);

        return disposer;
    });


    void TryStartService()
    {
        if (!HttpTransferService.IsStarted)
            this.platform.StartService(typeof(HttpTransferService), true);
    }
}