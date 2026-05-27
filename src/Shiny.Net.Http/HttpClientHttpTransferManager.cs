using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;
using Shiny.Extensions.Stores.Repositories;
using Shiny.Net.Http.Infrastructure;

namespace Shiny.Net.Http;


public class HttpClientHttpTransferManager(
    HttpClientHttpTransferProcess process,
    ILogger<HttpClientHttpTransferManager> logger,
    IRepository repository
) : IHttpTransferManager, IShinyStartupTask, IDisposable
{
    static bool isRunning;
    bool subscribed;

    public void Start()
    {
        if (!this.subscribed)
        {
            HttpClientHttpTransferProcess.ProgressOccurred += this.OnProcessProgress;
            repository.ActionOccurred += this.OnRepoAction;
            this.subscribed = true;
        }

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
    }

    public void Dispose()
    {
        if (this.subscribed)
        {
            HttpClientHttpTransferProcess.ProgressOccurred -= this.OnProcessProgress;
            repository.ActionOccurred -= this.OnRepoAction;
            this.subscribed = false;
        }
    }

    void OnProcessProgress(object? sender, HttpTransferResult result)
        => this.UpdateReceived?.Invoke(this, result);

    void OnRepoAction(object? sender, (RepositoryAction Action, Type EntityType, IRepositoryEntity? Entity) x)
    {
        if (x.EntityType != typeof(HttpTransfer) || x.Action == RepositoryAction.Update)
            return;
        this.CountChanged?.Invoke(this, repository.GetAll<HttpTransfer>().Count);
    }


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

            if (transfer.Request.Type == TransferType.Download)
            {
                try
                {
                    if (File.Exists(transfer.Request.LocalFilePath))
                        File.Delete(transfer.Request.LocalFilePath);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to remove partial download file for cancelled transfer {id}", identifier);
                }
            }

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
        var transfers = repository.GetAll<HttpTransfer>();
        repository.Clear<HttpTransfer>();

        foreach (var transfer in transfers)
        {
            if (transfer.Request.Type == TransferType.Download)
            {
                try
                {
                    if (File.Exists(transfer.Request.LocalFilePath))
                        File.Delete(transfer.Request.LocalFilePath);
                }
                catch (Exception ex)
                {
                    logger.LogWarning(ex, "Failed to remove partial download file for cancelled transfer {id}", transfer.Identifier);
                }
            }
        }
        return Task.CompletedTask;
    }


    public event EventHandler<int>? CountChanged;
    public event EventHandler<HttpTransferResult>? UpdateReceived;


    void TryStartProcess()
    {
        if (!isRunning)
        {
            isRunning = true;
            process.Run(() => isRunning = false);
        }
    }
}
