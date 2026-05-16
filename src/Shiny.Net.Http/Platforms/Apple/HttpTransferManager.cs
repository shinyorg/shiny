using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Text;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;
using Shiny.Support.Repositories;

namespace Shiny.Net.Http;


public partial class HttpTransferManager(
    ILogger<HttpTransferManager> logger,
    IRepository repository,
    IPlatform platform,
    IServiceProvider services,
    INativeConfigurator? configurator = null
) : 
    NSUrlSessionDownloadDelegate,
    IHttpTransferManager,
    IShinyStartupTask,
    IIosLifecycle.IHandleEventsForBackgroundUrl
{
    Action? completionHandler;

    public event EventHandler<int>? CountChanged;
    public event EventHandler<HttpTransferResult>? UpdateReceived;


    public async void Start()
    {
        repository.ActionOccurred += this.OnRepoAction;

        try
        {
            var tasks = await this.Session.GetAllTasksAsync();
            foreach (var task in tasks)
                task.Resume();
        }
        catch (Exception ex)
        {
            logger.LogError(ex, "Error auto-starting HTTP Transfers");
        }
    }

    void OnRepoAction(object? sender, (RepositoryAction Action, Type EntityType, IRepositoryEntity? Entity) x)
    {
        if (x.EntityType != typeof(HttpTransfer) || x.Action == RepositoryAction.Update)
            return;
        this.CountChanged?.Invoke(this, repository.GetAll<HttpTransfer>().Count);
    }


    NSUrlSession? nsUrlSession;
    public NSUrlSession Session
    {
        get
        {
            if (this.nsUrlSession == null)
            {
                var sessionName = $"{NSBundle.MainBundle.BundleIdentifier}.Shiny";
                var cfg = NSUrlSessionConfiguration.CreateBackgroundSessionConfiguration(sessionName);
                cfg.HttpMaximumConnectionsPerHost = 2;
                configurator?.Configure(cfg);
                cfg.SessionSendsLaunchEvents = true;
                this.nsUrlSession = NSUrlSession.FromConfiguration(cfg, this, new NSOperationQueue());
            }
            return this.nsUrlSession!;
        }
    }


    public Task<IList<HttpTransfer>> GetTransfers()
    {
        var transfers = repository.GetAll<HttpTransfer>().ToList();
        return Task.FromResult<IList<HttpTransfer>>(transfers);
    }


    public async Task<HttpTransfer> Queue(HttpTransferRequest request)
    {
        request.AssertValid();
        try
        {
            var transfer = new HttpTransfer(request, 0, 0, HttpTransferState.Pending, DateTimeOffset.UtcNow);
            repository.Insert(transfer);

            NSUrlSessionTask task;

            if (request.Type.IsUpload())
            {
                task = await this.CreateUpload(request).ConfigureAwait(false);
            }
            else
            {
                var nativeRequest = request.ToNative();
                configurator?.Configure(nativeRequest, request);
                task = this.Session.CreateDownloadTask(nativeRequest);
                
            }

            task.TaskDescription = request.Identifier;
            task.Resume();

            return transfer;
        }
        catch
        {
            repository.Remove<HttpTransfer>(request.Identifier);
            throw;
        }
    }


    public async Task Cancel(string identifier)
    {
        var task = (await this.Session.GetAllTasksAsync())
            .FirstOrDefault(x => x
                .TaskDescription!
                .Equals(identifier, StringComparison.InvariantCultureIgnoreCase)
            );

        if (task != null)
            task.Cancel();
    }


    public async Task CancelAll()
    {
        var tasks = await this.Session.GetAllTasksAsync();
        foreach (var task in tasks)
            task.Cancel();
    }




    public bool Handle(string sessionIdentifier, Action incomingCompletionHandler)
    {
        //if (this.SessionName.Equals(sessionIdentifier))
        //{
        //    this.completionHandler = incomingCompletionHandler;
        //    return true;
        //}
        //return false;
        this.completionHandler = incomingCompletionHandler; // TODO
        return true;
    }


    void Remove(HttpTransfer ht)
    {
        repository.Remove(ht);
        this.TryDeleteUploadTempFile(ht);
    }


    async void OnError(HttpTransfer ht, int statusCode, Exception ex)
    {
        logger.LogError(ex, $"Transfer {ht.Identifier} was completed with error");
        this.Remove(ht);

        await services
            .RunDelegates<IHttpTransferDelegate>(
                x => x.OnError(ht.Request, statusCode, ex),
                logger
            );

        this.UpdateReceived?.Invoke(this, new(
            ht.Request,
            HttpTransferState.Error,
            TransferProgress.Empty,
            ex
        ));
        this.TryCompleteSession();
    }


    void OnCancel(HttpTransfer ht)
    {
        logger.LogInformation($"Transfer {ht.Identifier} was canceled");
        this.Remove(ht);

        this.UpdateReceived?.Invoke(this, new(
            ht.Request,
            HttpTransferState.Canceled,
            TransferProgress.Empty,
            null
        ));
        this.TryCompleteSession();
    }


    async void OnFinish(HttpTransfer ht)
    {
        this.Remove(ht);
        await services.RunDelegates<IHttpTransferDelegate>(x => x.OnCompleted(ht.Request), logger);

        this.UpdateReceived?.Invoke(this, new(
            ht.Request,
            HttpTransferState.Completed,
            TransferProgress.Empty,
            null
        ));
        this.TryCompleteSession();
    }


    async Task<NSUrlSessionTask> CreateUpload(HttpTransferRequest request)
    {
        var httpMethod = request.GetHttpMethod();
        if (httpMethod != HttpMethod.Post && httpMethod != HttpMethod.Put)
            throw new ArgumentException($"Invalid Upload HTTP Verb {request.HttpMethod} - only PUT or POST are valid");
        
        var native = request.ToNative();
        configurator?.Configure(native, request);

        NSUrlSessionUploadTask task = null!;
        if (request.Type == TransferType.UploadMultipart)
        {
            task = await this.UploadAsMultipartRequest(request, native).ConfigureAwait(false);
        }
        else
        {
            if (request.HttpContent != null)
                throw new InvalidOperationException("HttpContent cannot be sent for raw uploads");

            // native.Body = NSData.From
            var fileUrl = NSUrl.CreateFileUrl(request.LocalFilePath, null);
            task = this.Session.CreateUploadTask(native, fileUrl);
        }

        return task;
    }


    async Task<NSUrlSessionUploadTask> UploadAsMultipartRequest(HttpTransferRequest request, NSMutableUrlRequest native)
    {
        var tempPath = platform.GetUploadTempFilePath(request);
        logger.LogInformation("Writing temp form data body to {Path}", tempPath);
        
        var content = new MultipartFormDataContent();

        if (request.HttpContent != null)
        {
            content.Add(
                new StringContent(
                    request.HttpContent.Content,
                    Encoding.GetEncoding(request.HttpContent.Encoding),
                    request.HttpContent.ContentType
                ),
                request.HttpContent.ContentFormDataName ?? "value"
            );
        }
   
        await using (var uploadFile = File.OpenRead(request.LocalFilePath))
        {
            var fileName = Path.GetFileName(request.LocalFilePath);
            
            var innerContent = new StreamContent(uploadFile);
            content.Add(innerContent, request.FileFormDataName, fileName);
            innerContent.Headers.ContentDisposition!.FileNameStar = null;

            await using (var fs = File.Create(tempPath))
                await content.CopyToAsync(fs);
        }

        logger.LogInformation("Form body written");
        native["Content-Type"] = content.Headers.ContentType!.ToString();
        
        var tempFileUrl = NSUrl.CreateFileUrl(tempPath, null);
        var task = this.Session.CreateUploadTask(native, tempFileUrl);
        return task;
    }

    
    void TryDeleteUploadTempFile(HttpTransfer transfer)
    {
        if (!transfer.Request.Type.IsUpload())
            return;

        var path = platform.GetUploadTempFilePath(transfer.Request);

        if (File.Exists(path))
        {
            try
            {
                logger.LogDebug($"Deleting temporary upload file - {transfer.Identifier}");

                // sometimes iOS will hold a file lock a bit longer than it should
                File.Delete(path);
                logger.LogDebug($"Temporary upload file deleted - {transfer.Identifier}");
            }
            catch (Exception ex)
            {
                logger.LogWarning($"Unable to delete temporary upload file - {transfer.Identifier}", ex);
            }
        }
    }


    void TryCompleteSession()
    {
        var transfers = repository.GetAll<HttpTransfer>();
        if (transfers.Count == 0)
        {
            this.completionHandler?.Invoke();
            this.nsUrlSession = null;
        }
    }
}