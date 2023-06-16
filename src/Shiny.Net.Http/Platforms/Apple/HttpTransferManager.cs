using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;
using Shiny.Support.Repositories;

namespace Shiny.Net.Http;


public class HttpTransferManager : NSUrlSessionDownloadDelegate,
                                   IHttpTransferManager,
                                   IIosLifecycle.IHandleEventsForBackgroundUrl
{
    const string SessionName = "Shiny";
    Action? completionHandler;

    readonly ILogger logger;
    readonly IPlatform platform;
    readonly IRepository repository;
    readonly IServiceProvider services;


    public HttpTransferManager(
        AppleConfiguration config,
        ILogger<HttpTransferManager> logger,
        IRepository repository,
        IPlatform platform,
        IServiceProvider services
    )
    {
        this.logger = logger;
        this.repository = repository;
        this.platform = platform;
        this.services = services;

        var cfg = NSUrlSessionConfiguration.CreateBackgroundSessionConfiguration(SessionName);
        if (config.HttpMaximumConnectionsPerHost != null)
            cfg.HttpMaximumConnectionsPerHost = config.HttpMaximumConnectionsPerHost.Value;

        if (config.CachePolicy != null)
            cfg.RequestCachePolicy = config.CachePolicy.Value;

        if (config.HttpShouldUsePipelining != null)
            cfg.HttpShouldUsePipelining = config.HttpShouldUsePipelining.Value;

        cfg.AllowsCellularAccess = config.AllowsCellularAccess;
        cfg.AllowsConstrainedNetworkAccess = config.AllowsConstrainedNetworkAccess;
        cfg.AllowsExpensiveNetworkAccess = config.AllowsExpensiveNetworkAccess;

        cfg.SessionSendsLaunchEvents = true;
        //cfg.SharedContainerIdentifier
        //cfg.ShouldUseExtendedBackgroundIdleMode
        //cfg.WaitsForConnectivity

        this.Session = NSUrlSession.FromConfiguration(cfg, this, new NSOperationQueue());
        //this.Session.CreateDownloadTaskFromResumeData
    }


    public NSUrlSession Session { get; } // TODO: when session completes/invalidates - we may need to be able to create new one


    public Task<IList<HttpTransfer>> GetTransfers()
    {
        var transfers = this.repository.GetList<HttpTransfer>();
        return Task.FromResult(transfers);
    }


    public async Task<HttpTransfer> Queue(HttpTransferRequest request)
    {
        request.AssertValid();
        if (this.repository.Exists<HttpTransfer>(request.Identifier))
            throw new ArgumentException("A request already exists with this identifier: " + request.Identifier);

        NSUrlSessionTask task;
        var nativeRequest = request.ToNative();

        if (request.IsUpload)
        {
            var tempFileUri = await this.CreateUploadTempFile(request).ConfigureAwait(false);
            task = this.Session.CreateUploadTask(nativeRequest, tempFileUri);
        }
        else
        {
            task = this.Session.CreateDownloadTask(nativeRequest);
        }
        task.TaskDescription = request.Identifier;
        task.Resume();
        //task.Response
        //task.Progress

        var transfer = new HttpTransfer(request, 0, 0, HttpTransferState.Pending, DateTimeOffset.UtcNow);
        this.repository.Insert(transfer);

        return transfer;
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

        this.repository.Remove<HttpTransfer>(identifier);
    }


    public async Task CancelAll()
    {
        this.repository.Clear<HttpTransfer>();
        var tasks = await this.Session.GetAllTasksAsync();
        foreach (var task in tasks)
            task.Cancel();
    }


    readonly Subject<HttpTransfer> transferSubj = new();
    public IObservable<HttpTransferResult> WhenUpdateReceived() => Observable.Create<HttpTransferResult>(ob => this
        .transferSubj
        .Timestamp()
        .Buffer(TimeSpan.FromSeconds(2))
        .Where(x => x.Count >= 2)
        .Subscribe(results =>
        {
            var groups = results
                .OrderByDescending(x => x.Timestamp)
                .GroupBy(x => new { x.Value.Identifier })
                .ToList();

            // group by identifier, take first and last by timestamp, then take diff in bytes transferred to determine bytes per second, estimate type, etc
            foreach (var group in groups)
            {
                // I need to have at least two in a group as well
                var first = group.First();
                var last = group.Last();

                var ts = last.Timestamp.Subtract(first.Timestamp);
                var totalXfer = last.Value.BytesTransferred - first.Value.BytesTransferred;
                var bytesPerSecond = 0L;

                if (totalXfer > 0)
                    bytesPerSecond = Convert.ToInt64(Math.Round(totalXfer / ts.TotalSeconds));
                
                ob.OnNext(new HttpTransferResult(
                    last.Value.Request,
                    last.Value.Status,
                    new TransferProgress(
                        bytesPerSecond,
                        last.Value.BytesToTransfer,
                        last.Value.BytesTransferred
                    )
                ));
            }
        }));


    public bool Handle(string sessionIdentifier, Action incomingCompletionHandler)
    {
        if (SessionName.Equals(sessionIdentifier))
        {
            this.completionHandler = incomingCompletionHandler;
            return true;
        }
        return false;
    }


    async Task<NSUrl> CreateUploadTempFile(HttpTransferRequest request)
    {
        var httpMethod = new HttpMethod(request.HttpMethod ?? "GET");
        if (httpMethod != HttpMethod.Post && httpMethod != HttpMethod.Put)
            throw new ArgumentException($"Invalid Upload HTTP Verb {request.HttpMethod} - only PUT or POST are valid");

        var boundary = Guid.NewGuid().ToString("N");

        var native = request.ToNative();
        native["Content-Type"] = $"multipart/form-data; boundary={boundary}";

        var tempPath = this.platform.GetUploadTempFilePath(request);

        this.logger.LogInformation("Writing temp form data body to " + tempPath);

        using var fs = new FileStream(tempPath, FileMode.Create);
        if (!request.PostData.IsEmpty())
        {
            fs.WriteString("--" + boundary);
            fs.WriteString("Content-Type: text/plain; charset=utf-8");
            fs.WriteString("Content-Disposition: form-data;");
            fs.WriteLine();
            fs.WriteString(request.PostData!);
            fs.WriteLine();
        }

        using var uploadFile = File.OpenRead(request.LocalFilePath);
        fs.WriteString("--" + boundary);
        fs.WriteString("Content-Type: application/octet-stream");
        fs.WriteString($"Content-Disposition: form-data; name=\"blob\"; filename=\"{request.LocalFilePath}\"");
        fs.WriteLine();
        await uploadFile.CopyToAsync(fs);

        fs.WriteLine();
        fs.WriteString($"--{boundary}--");

        this.logger.LogInformation("Form body written");
        var tempFileUrl = NSUrl.CreateFileUrl(tempPath, null);

        return tempFileUrl;
    }


    public override void DidBecomeInvalid(NSUrlSession session, NSError error)
    {
        this.logger.LogDebug($"DidBecomeInvalid");
        //this.manager.CompleteSession(); // TODO: restart session?
        if (error != null)
            this.logger.LogError(new InvalidOperationException(error.LocalizedDescription), "DidBecomeInvalid reported an error");
    }


    //// this is tough to implement due to NSInputStream & CFStream delegates
    //// reauthorize?
    ////public override void NeedNewBodyStream(NSUrlSession session, NSUrlSessionTask task, Action<NSInputStream> completionHandler)
    ////{
    ////    var transfer = task.FromNative();
    ////    var file = new FileInfo(transfer.LocalFilePath);
    ////    //var stream = new BodyStream(file);
    ////    //completionHandler(stream);
    ////}


    ////public override void DidReceiveChallenge(NSUrlSession session, NSUrlAuthenticationChallenge challenge, Action<NSUrlSessionAuthChallengeDisposition, NSUrlCredential> completionHandler)
    ////{
    ////    this.logger.LogDebug($"DidReceiveChallenge");
    ////    completionHandler.Invoke(NSUrlSessionAuthChallengeDisposition.PerformDefaultHandling, null!);
    ////}


    ////public override void DidReceiveChallenge(NSUrlSession session, NSUrlSessionTask task, NSUrlAuthenticationChallenge challenge, Action<NSUrlSessionAuthChallengeDisposition, NSUrlCredential> completionHandler)
    ////{
    ////    this.logger.LogDebug($"DidReceiveChallenge for task");
    ////    completionHandler.Invoke(NSUrlSessionAuthChallengeDisposition.PerformDefaultHandling, null!);
    ////}


    //public override void DidResume(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long resumeFileOffset, long expectedTotalBytes)
    //{
    //    this.logger.LogDebug("DidResume");
    //    //this.onEvent.OnNext(downloadTask.FromNative());
    //}


    public override void DidFinishEventsForBackgroundSession(NSUrlSession session)
    {
        this.logger.LogDebug("DidFinishEventsForBackgroundSession");
        this.completionHandler?.Invoke();
    }


    public override async void DidCompleteWithError(NSUrlSession session, NSUrlSessionTask task, NSError? error)
    {
        this.logger.LogDebug("DidCompleteWithError");
        var ht = this.repository.Get<HttpTransfer>(task.TaskDescription);
        if (ht == null)
        {
            this.logger.LogDebug("No transfer was found - " + task.TaskDescription);
            return;
        }

        switch (task.State)
        {
            //case NSUrlSessionTaskState.Running:
            //    ht = ht with { Status = HttpTransferState.InProgress };
            //    this.repository.Set(ht);
            //    break;

            //case NSUrlSessionTaskState.Suspended:
            //    ht = ht with { Status = HttpTransferState.Paused };
            //    this.repository.Set(ht);
            //    break;

            case NSUrlSessionTaskState.Canceling:
                this.logger.LogWarning($"Transfer {ht.Identifier} was cancelled");
                this.repository.Remove<HttpTransfer>(ht.Identifier);

                ht = ht with { Status = HttpTransferState.Canceled };
                break;

            case NSUrlSessionTaskState.Completed:
                // TODO: error?
                this.logger.LogInformation($"Transfer {ht.Identifier} completed successfully");
                await this.services.RunDelegates<IHttpTransferDelegate>(x => x.OnCompleted(ht.Request), this.logger);
                this.repository.Remove<HttpTransfer>(ht.Identifier);

                ht = ht with { Status = HttpTransferState.Completed };
                break;

            default:
                var ex = new InvalidOperationException(error.LocalizedDescription);
                this.logger.LogError(ex, "Error with HTTP transfer: " + ht.Identifier);
                await this.services.RunDelegates<IHttpTransferDelegate>(x => x.OnError(ht.Request, ex), this.logger);
                break;
        }
        this.transferSubj.OnNext(ht);
        this.TryDeleteUploadTempFile(ht);
    }


    public override void DidSendBodyData(NSUrlSession session, NSUrlSessionTask task, long bytesSent, long totalBytesSent, long totalBytesExpectedToSend)
    {
        this.logger.LogDebug("DidSendBodyData");
        var ht = this.repository.Get<HttpTransfer>(task.TaskDescription);

        if (ht != null)
        {
            ht = ht with
            {
                BytesToTransfer = totalBytesExpectedToSend,
                BytesTransferred = totalBytesSent,
                Status = HttpTransferState.InProgress
            };
            this.repository.Set(ht);
            this.transferSubj.OnNext(ht);
        }
    }


    public override void DidWriteData(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long bytesWritten, long totalBytesWritten, long totalBytesExpectedToWrite)
    {
        this.logger.LogDebug("DidWriteData");
        var ht = this.repository.Get<HttpTransfer>(downloadTask.TaskDescription);

        if (ht != null)
        {
            ht = ht with
            {
                BytesToTransfer = totalBytesExpectedToWrite,
                BytesTransferred = totalBytesWritten,
                Status = HttpTransferState.InProgress
            };
            this.repository.Set(ht);
            this.transferSubj.OnNext(ht);
        }
    }


    public override async void DidFinishDownloading(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, NSUrl location)
    {
        this.logger.LogDebug("DidFinishDownloading");
        var transfer = this.repository.Get<HttpTransfer>(downloadTask.TaskDescription);

        if (transfer != null)
        {
            File.Copy(location.Path!, transfer.Request.LocalFilePath, true);
            this.repository.Remove<HttpTransfer>(transfer.Identifier);

            await this.services.RunDelegates<IHttpTransferDelegate>(
                x => x.OnCompleted(transfer.Request),
                this.logger
            );
            this.transferSubj.OnNext(transfer with
            {
                Status = HttpTransferState.Completed
            });
        }
    }


    void TryDeleteUploadTempFile(HttpTransfer transfer)
    {
        if (!transfer.Request.IsUpload)
            return;

        var path = this.platform.GetUploadTempFilePath(transfer.Request);

        if (File.Exists(path))
        {
            try
            {
                this.logger.LogDebug($"Deleting temporary upload file - {transfer.Identifier}");

                // sometimes iOS will hold a file lock a bit longer than it should
                File.Delete(path);
                this.logger.LogDebug($"Temporary upload file deleted - {transfer.Identifier}");
            }
            catch (Exception ex)
            {
                this.logger.LogWarning($"Unable to delete temporary upload file - {transfer.Identifier}", ex);
            }
        }
    }
}