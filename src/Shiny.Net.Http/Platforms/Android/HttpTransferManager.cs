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


public class HttpTransferManager(
    AndroidPlatform platform,
    ILogger<HttpTransferManager> logger,
    IRepository repository
) : IHttpTransferManager, IShinyStartupTask
{
    public void Start()
    {
        try
        {
            if (HttpTransferService.IsStarted)
                return;

            var transfers = repository.GetList<HttpTransfer>();
            if (transfers.Count > 0)
                this.TryStartService();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to auto-start HTTP Transfer Manager");
        }
    }


    public Task<IList<HttpTransfer>> GetTransfers()
    {
        var transfers = repository.GetList<HttpTransfer>();
        return Task.FromResult(transfers);
    }


    public async Task<HttpTransfer> Queue(HttpTransferRequest request)
    {
        request.AssertValid();
        (await platform.RequestForegroundServicePermissions()).Assert(allowRestricted: true);
        if (OperatingSystemShim.IsAndroidVersionAtLeast(34))
        {
            (await platform.RequestAccess("android.permission.FOREGROUND_SERVICE_DATA_SYNC").ToTask()).Assert();
        }
        // this will trigger over to the job if it is running
        long? contentLength = null;
        if (request.Type.IsUpload())
        {
            var file = new FileInfo(request.LocalFilePath);
            if (!file.Exists)
                throw new InvalidOperationException("File to be uploaded does not exist");
            
            contentLength = file.Length;
        }
        else
        {
            var dir = Path.GetDirectoryName(request.LocalFilePath);
            if (!Directory.Exists(dir))
                throw new InvalidOperationException("Download directory does not exist");
        }
        

        var transfer = new HttpTransfer(
            request,
            contentLength,
            0,
            HttpTransferState.Pending,
            DateTimeOffset.UtcNow
        );
        repository.Insert(transfer);
        this.TryStartService();

        return transfer;
    }


    public Task Cancel(string identifier)
    {
        // this will trigger over to the foreground service which will shut itself down if there are no other transfers
        var transfer = repository.Get<HttpTransfer>(identifier);
        if (transfer != null)
        {
            repository.Remove(transfer);

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
        repository.Clear<HttpTransfer>();
        return Task.CompletedTask;
    }


    public IObservable<int> WatchCount() => repository.CreateCountWatcher<HttpTransfer>();

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
            platform.StartService(typeof(HttpTransferService), true);
    }
}