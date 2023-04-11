using System;
using System.Collections.Generic;
using System.IO;
using System.Threading.Tasks;
using Shiny.Jobs;
using Shiny.Support.Repositories;

namespace Shiny.Net.Http;


public class HttpTransferManager : IHttpTransferManager
{
    readonly IJobManager jobManager;
    readonly IRepository repository;


    public HttpTransferManager(
        IJobManager jobManager,
        IRepository repository
    )
    {
        this.jobManager = jobManager;
        this.repository = repository;
    }


    public ValueTask<IList<HttpTransfer>> GetTransfers()
    {
        var transfers = this.repository.GetList<HttpTransfer>();
        return ValueTask.FromResult(transfers);
    }


    public async ValueTask<HttpTransfer> Queue(HttpTransferRequest request)
    {
        (await this.jobManager.RequestAccess()).Assert();
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
        return transfer;
    }


    public ValueTask Cancel(string identifier)
    {
        // this will trigger over to the job if it is running
        this.repository.Remove<HttpTransfer>(identifier); 
        return ValueTask.CompletedTask;
    }


    public ValueTask CancelAll()
    {
        // this will trigger over to the job if it is running
        this.repository.Clear<HttpTransfer>();
        return ValueTask.CompletedTask;
    }


    public IObservable<HttpTransferResult> WhenUpdateReceived() => TransferJob.WhenProgress();
}