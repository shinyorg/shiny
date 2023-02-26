using System;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;

namespace Shiny.Net.Http;


public class HttpTransferManager : NSUrlSessionDownloadDelegate,
                                   IHttpTransferManager,
                                   IShinyStartupTask,
                                   IIosLifecycle.IHandleEventsForBackgroundUrl
{
    const string SessionName = "Shiny";
    Action? completionHandler;

    readonly ILogger logger;
    readonly IPlatform platform;
    readonly IServiceProvider services;


    public HttpTransferManager(
        ILogger<HttpTransferManager> logger,
        IPlatform platform,
        IServiceProvider services
    )
    {
        this.logger = logger;
        this.platform = platform;
        this.services = services;

        var cfg = NSUrlSessionConfiguration.CreateBackgroundSessionConfiguration(SessionName);

        this.Session = NSUrlSession.FromConfiguration(cfg, this, new NSOperationQueue());
        // TODO: allow OS configuration
        //        this.sessionConfig.HttpMaximumConnectionsPerHost = maxConnectionsPerHost;
        //        this.sessionConfig.RequestCachePolicy = NSUrlRequestCachePolicy.ReloadIgnoringLocalAndRemoteCacheData;
    }


    public async void Start()
    {
        try
        {
            var tasks = await this.Session.GetAllTasksAsync();
            foreach (var task in tasks)
            {
                // TODO: I actually need both the temp file and the regular file - if upload
                // TODO: force resume? probably
                var upload = task.Description?.StartsWith("BackgroundUploadTask") ?? false;

                var request = new HttpTransferRequest(
                    task.OriginalRequest!.Url.ToString(),
                    upload,
                    task.TaskDescription!,
                    task.OriginalRequest.AllowsExpensiveNetworkAccess,
                    task.OriginalRequest.Body?.ToString(),
                    task.OriginalRequest.HttpMethod,
                    task.OriginalRequest.Headers?.FromNsDictionary()
                );
                var ht = new HttpTransfer(
                    request,
                    task
                );

                this.transfers.Add(ht);
            }
        }
        catch (Exception ex)
        {
            this.logger.LogError(ex, "Could not restart HTTP Transfers");
        }
    }


    public NSUrlSession Session { get; } // TODO: when session completes/invalidates - we may need to be able to create new one

    readonly ObservableList<IHttpTransfer> transfers = new();
    public INotifyReadOnlyCollection<IHttpTransfer> Transfers => this.transfers;


    public async Task<IHttpTransfer> Queue(HttpTransferRequest request)
    {
        NSUrlSessionTask task;
        var nativeRequest = request.ToNative();

        if (request.IsUpload)
        {
            // TODO: I actually need both the temp file and the regular file
            //var fileUri = NSUrl.FromFilename(request.LocalFile.FullName);
            var tempFileUri = await this.CreateUploadTempFile(request).ConfigureAwait(false);
            task = this.Session.CreateUploadTask(nativeRequest, tempFileUri);
            task.TaskDescription = request.LocalFilePath;
        }
        else
        {
            task = this.Session.CreateDownloadTask(nativeRequest);
        }

        task.TaskDescription = request.LocalFilePath;
        var ht = new HttpTransfer(request, task);
        this.transfers.Add(ht);
        ht.Resume();

        return ht;
    }


    public Task CancelAll()
    {
        foreach (HttpTransfer transfer in this.Transfers)
            transfer.Cancel();

        this.transfers.Clear();
        return Task.CompletedTask;
    }


    public Task Cancel(string identifier)
    {
        var transfer = this.Get(identifier);
        if (transfer != null)
        {
            transfer.Cancel();
            this.transfers.Remove(transfer);
        }
        return Task.CompletedTask;
    }


    public Task Pause(string identifier)
    {
        this.Get(identifier)?.Pause();
        return Task.CompletedTask;
    }


    public Task Resume(string identifier)
    {
        this.Get(identifier)?.Resume();
        return Task.CompletedTask;
    }


    public bool Handle(string sessionIdentifier, Action completionHandler)
    {
        if (SessionName.Equals(sessionIdentifier))
        {
            this.completionHandler = completionHandler;
            return true;
        }
        return false;
    }


    HttpTransfer? Get(string identifier)
        => this.transfers.FirstOrDefault(x => x.Identifier.Equals(identifier)) as HttpTransfer;


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
            this.logger.LogError(new Exception(error.LocalizedDescription), "DidBecomeInvalid reported an error");
    }


    // this is tough to implement due to NSInputStream & CFStream delegates
    // reauthorize?
    //public override void NeedNewBodyStream(NSUrlSession session, NSUrlSessionTask task, Action<NSInputStream> completionHandler)
    //{
    //    var transfer = task.FromNative();
    //    var file = new FileInfo(transfer.LocalFilePath);
    //    //var stream = new BodyStream(file);
    //    //completionHandler(stream);
    //}


    //public override void DidReceiveChallenge(NSUrlSession session, NSUrlAuthenticationChallenge challenge, Action<NSUrlSessionAuthChallengeDisposition, NSUrlCredential> completionHandler)
    //{
    //    this.logger.LogDebug($"DidReceiveChallenge");
    //    completionHandler.Invoke(NSUrlSessionAuthChallengeDisposition.PerformDefaultHandling, null!);
    //}


    //public override void DidReceiveChallenge(NSUrlSession session, NSUrlSessionTask task, NSUrlAuthenticationChallenge challenge, Action<NSUrlSessionAuthChallengeDisposition, NSUrlCredential> completionHandler)
    //{
    //    this.logger.LogDebug($"DidReceiveChallenge for task");
    //    completionHandler.Invoke(NSUrlSessionAuthChallengeDisposition.PerformDefaultHandling, null!);
    //}

    public override void DidFinishEventsForBackgroundSession(NSUrlSession session)
    {
        this.logger.LogDebug($"DidFinishEventsForBackgroundSession");
        this.completionHandler?.Invoke();
    }


    public override void DidCompleteWithError(NSUrlSession session, NSUrlSessionTask task, NSError error)
    {
        this.logger.LogDebug($"DidCompleteWithError");
        var ht = this.Get(task.TaskIdentifier.ToString());
        if (ht == null)
            return;

        //var transfer = task.FromNative();

        //switch (transfer.Status)
        //{
        //    case HttpTransferState.Canceled:
        //        this.logger.LogWarning($"Transfer {transfer.Identifier} was cancelled");
        //        break;

        //    case HttpTransferState.Completed:
        //        this.logger.LogInformation($"Transfer {transfer.Identifier} completed successfully");
        //        //await this.shinyDelegates.Value.RunDelegates(
        //        //    x => x.OnCompleted(transfer)
        //        //);
        //        break;

        //    default:
        //        //await this.HandleError(transfer, error);
        //        break;
        //}
        this.TryDeleteUploadTempFile(ht);
        //this.onEvent.OnNext(transfer);
    }


    public override void DidSendBodyData(NSUrlSession session, NSUrlSessionTask task, long bytesSent, long totalBytesSent, long totalBytesExpectedToSend)
    {
        this.logger.LogDebug($"DidSendBodyData");
        this.Get(task.TaskIdentifier.ToString())?.OnBytesTransferred.OnNext(bytesSent);
    }


    public override void DidWriteData(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long bytesWritten, long totalBytesWritten, long totalBytesExpectedToWrite)
    {
        this.logger.LogDebug("DidWriteData");
        this.Get(downloadTask.TaskIdentifier.ToString())?.OnBytesTransferred.OnNext(bytesWritten);
    }


    public override void DidResume(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long resumeFileOffset, long expectedTotalBytes)
    {
        this.logger.LogDebug("DidResume");
        //this.onEvent.OnNext(downloadTask.FromNative());
    }


    public override async void DidFinishDownloading(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, NSUrl location)
    {
        this.logger.LogDebug("DidFinishDownloading");
        var ht = this.transfers.FirstOrDefault(x => x.Identifier.Equals(downloadTask.TaskDescription));
        if (ht != null)
        {
            //if (!ht.LocalFilePath.IsEmpty())
            //    // if you are debugging, the base path tends to change, so the destination path changes too
            //    File.Copy(location.Path, transfer.LocalFilePath, true);

            // TODO: should thread safety collection
            this.transfers.Remove(ht);
            await this.services.RunDelegates<IHttpTransferDelegate>(
                x => x.OnCompleted(ht),
                this.logger
            );
        }
    }


    //protected virtual async Task HandleError(HttpTransfer transfer, NSError error)
    //{
    //    this.logger.LogError(transfer.Exception, "Error with HTTP transfer: " + transfer.Identifier);
    //    await this.shinyDelegates.Value.RunDelegates(
    //        x => x.OnError(transfer, transfer.Exception)
    //    );
    //}


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