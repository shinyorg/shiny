using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Support.Repositories;

namespace Shiny.Net.Http;


public class HttpTransferManager(
    HttpTransferProcess process,
    ILogger<HttpTransferManager> logger,
    IRepository repository
) : IHttpTransferManager, IShinyStartupTask
{
    static bool isRunning;

    public void Start()
    {
        try
        {
            if (isRunning)
                return;

            var transfers = repository.GetAll<HttpTransfer>();
            if (transfers.Count > 0)
                this.TryStartProcess();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Failed to auto-start HTTP Transfer Manager");
        }

        HttpTransferProcess.ProgressOccurred += this.OnProcessProgress;
    }

    void OnProcessProgress(object? sender, HttpTransferResult result)
        => this.UpdateReceived?.Invoke(this, result);


    public Task<IList<HttpTransfer>> GetTransfers()
    {
        var transfers = repository.GetAll<HttpTransfer>().ToList();
        return Task.FromResult<IList<HttpTransfer>>(transfers);
    }


    public Task<HttpTransfer> Queue(HttpTransferRequest request)
    {
        request.AssertValid();

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
        this.TryStartProcess();

        return Task.FromResult(transfer);
    }


    public Task Cancel(string identifier)
    {
        var transfer = repository.Get<HttpTransfer>(identifier);
        if (transfer != null)
        {
            repository.Remove(transfer);

            this.UpdateReceived?.Invoke(this, new(
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
        repository.Clear<HttpTransfer>();
        return Task.CompletedTask;
    }


    public IObservable<int> WatchCount() => repository.CreateCountWatcher<HttpTransfer>();

    public event EventHandler<HttpTransferResult>? UpdateReceived;
    public IObservable<HttpTransferResult> WhenUpdateReceived() => new HttpTransferUpdateObservable(this);


    void TryStartProcess()
    {
        if (!isRunning)
        {
            isRunning = true;
            process.Run(() => isRunning = false);
        }
    }
}
