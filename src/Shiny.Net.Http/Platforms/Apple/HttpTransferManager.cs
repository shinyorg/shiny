using System;
using System.Linq;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Extensions.Logging;
using Shiny.Hosting;
using Shiny.Infrastructure;
using Shiny.Stores;

namespace Shiny.Net.Http;


public class HttpTransferManager : NSUrlSessionDownloadDelegate,
                                   IHttpTransferManager,
                                   IShinyStartupTask,
                                   IIosLifecycle.IHandleEventsForBackgroundUrl
{
    const string SessionName = "Shiny";
    Action? completionHandler;

    readonly ILogger logger;
    readonly IServiceProvider services;


    public HttpTransferManager(
        ILogger<HttpTransferManager> logger,
        IServiceProvider services
    )
    {
        this.logger = logger;
        this.services = services;

        var cfg = NSUrlSessionConfiguration.CreateBackgroundSessionConfiguration(SessionName);

        this.Session = NSUrlSession.FromConfiguration(cfg, this, new NSOperationQueue());
        //        this.sessionConfig.HttpMaximumConnectionsPerHost = maxConnectionsPerHost;
        //        this.sessionConfig.RequestCachePolicy = NSUrlRequestCachePolicy.ReloadIgnoringLocalAndRemoteCacheData;
    }


    public async void Start()
    {
        var tasks = await this.Session.GetAllTasksAsync();
        foreach (var task in tasks)
        {

            if (Guid.TryParse(task.TaskDescription!, out var id))
            {
                //this.uploadTask.Resume();
                //this.downloadTask.Resume();

                if (task is NSUrlSessionUploadTask upload)
                {

                    //upload.TaskIdentifier
                }
                else if (task is NSUrlSessionDownloadTask download)
                {

                }
            }
        }
    }


    public NSUrlSession Session { get; } // TODO: when session completes/invalidates - we may need to be able to create new one

    readonly ObservableList<IHttpTransfer> transfers = new();
    public INotifyReadOnlyCollection<IHttpTransfer> Transfers => this.transfers;


    public async Task<IHttpTransfer> Add(HttpTransferRequest request)
    {
        HttpTransfer ht;
        var url = NSUrl.FromString(request.Uri)!;

        if (request.IsUpload)
        {
            var fileUri = NSUrl.FromFilename(request.LocalFile.FullName);

            await this.Session
                .CreateUploadTaskAsync(NSUrlRequest.FromUrl(url), fileUri, out var task)
                .ConfigureAwait(false);

            task.TaskDescription = Guid.NewGuid().ToString();
            ht = new HttpTransfer(request, task);
        }
        else
        {
            await this.Session
                .CreateDownloadTaskAsync(url, out var task)
                .ConfigureAwait(false);

            task.TaskDescription = Guid.NewGuid().ToString();
            ht = new HttpTransfer(request, task);
        }
        this.transfers.Add(ht);
        ht.Resume();

        return ht;
    }


    public async Task Remove(string identifier)
    {
        var transfer = this.transfers.FirstOrDefault(x => x.Identifier.Equals(identifier));
        if (transfer != null)
        {
            transfer.Cancel();
            //this.transfers.Remove(transfer);
        }
    }


    public bool Handle(string sessionIdentifier, Action completionHandler)
    {
        if (SessionName.Equals(sessionIdentifier))
        {
            this.completionHandler = completionHandler;
            // TODO: call completion handler when all tasks complete
            return true;
        }
        return false;
    }

    //    protected override async Task<HttpTransfer> CreateUpload(HttpTransferRequest request)
    //    {
    //        if (request.HttpMethod != System.Net.Http.HttpMethod.Post && request.HttpMethod != System.Net.Http.HttpMethod.Put)
    //            throw new ArgumentException($"Invalid Upload HTTP Verb {request.HttpMethod} - only PUT or POST are valid");

    //        var boundary = Guid.NewGuid().ToString("N");

    //        var native = request.ToNative();
    //        native["Content-Type"] = $"multipart/form-data; boundary={boundary}";

    //        var tempPath = this.platform.GetUploadTempFilePath(request);

    //        this.logger.LogInformation("Writing temp form data body to " + tempPath);

    //        using var fs = new FileStream(tempPath, FileMode.Create);
    //        if (!request.PostData.IsEmpty())
    //        {
    //            fs.WriteString("--" + boundary);
    //            fs.WriteString("Content-Type: text/plain; charset=utf-8");
    //            fs.WriteString("Content-Disposition: form-data;");
    //            fs.WriteLine();
    //            fs.WriteString(request.PostData!);
    //            fs.WriteLine();
    //        }

    //        using var uploadFile = request.LocalFile.OpenRead();
    //        fs.WriteString("--" + boundary);
    //        fs.WriteString("Content-Type: application/octet-stream");
    //        fs.WriteString($"Content-Disposition: form-data; name=\"blob\"; filename=\"{request.LocalFile.Name}\"");
    //        fs.WriteLine();
    //        await uploadFile.CopyToAsync(fs);
    //        fs.WriteLine();
    //        fs.WriteString($"--{boundary}--");

    //        this.logger.LogInformation("Form body written");
    //        var tempFileUrl = NSUrl.CreateFileUrl(tempPath, null);

    //        var task = this.Session.CreateUploadTask(native, tempFileUrl);
    //        var taskId = TaskIdentifier.Create(request.LocalFile);
    //        task.TaskDescription = taskId.ToString();
    //        var transfer = task.FromNative();
    //        task.Resume();

    //        return transfer;
    //    }


    public override void DidBecomeInvalid(NSUrlSession session, NSError error)
    {
        this.logger.LogDebug($"DidBecomeInvalid");
        //this.manager.CompleteSession();
        if (error != null)
            this.logger.LogError(new Exception(error.LocalizedDescription), "DidBecomeInvalid reported an error");
    }


    //    // this is tough to implement due to NSInputStream & CFStream delegates
    //    // reauthorize?
    //    //public override void NeedNewBodyStream(NSUrlSession session, NSUrlSessionTask task, Action<NSInputStream> completionHandler)
    //    //{
    //    //    var transfer = task.FromNative();
    //    //    var file = new FileInfo(transfer.LocalFilePath);
    //    //    //var stream = new BodyStream(file);
    //    //    //completionHandler(stream);
    //    //}


    public override void DidReceiveChallenge(NSUrlSession session, NSUrlAuthenticationChallenge challenge, Action<NSUrlSessionAuthChallengeDisposition, NSUrlCredential> completionHandler)
    {
        this.logger.LogDebug($"DidReceiveChallenge");
        completionHandler.Invoke(NSUrlSessionAuthChallengeDisposition.PerformDefaultHandling, null);
    }


    public override void DidReceiveChallenge(NSUrlSession session, NSUrlSessionTask task, NSUrlAuthenticationChallenge challenge, Action<NSUrlSessionAuthChallengeDisposition, NSUrlCredential> completionHandler)
    {
        this.logger.LogDebug($"DidReceiveChallenge for task");
        completionHandler.Invoke(NSUrlSessionAuthChallengeDisposition.PerformDefaultHandling, null);
    }


    public override void DidFinishEventsForBackgroundSession(NSUrlSession session)
    {
        this.logger.LogDebug($"DidFinishEventsForBackgroundSession");
        this.completionHandler?.Invoke();
    }


    //    public override async void DidCompleteWithError(NSUrlSession session, NSUrlSessionTask task, NSError error)
    //    {
    //        this.logger.LogDebug($"DidCompleteWithError");
    //        var transfer = task.FromNative();

    //        switch (transfer.Status)
    //        {
    //            case HttpTransferState.Canceled:
    //                this.logger.LogWarning($"Transfer {transfer.Identifier} was cancelled");
    //                break;

    //            case HttpTransferState.Completed:
    //                this.logger.LogInformation($"Transfer {transfer.Identifier} completed successfully");
    //                await this.shinyDelegates.Value.RunDelegates(
    //                    x => x.OnCompleted(transfer)
    //                );
    //                break;

    //            default:
    //                await this.HandleError(transfer, error);
    //                break;
    //        }
    //        this.TryDeleteUploadTempFile(transfer);
    //        this.onEvent.OnNext(transfer);
    //    }


    //    public override void DidSendBodyData(NSUrlSession session, NSUrlSessionTask task, long bytesSent, long totalBytesSent, long totalBytesExpectedToSend)
    //    {
    //        this.logger.LogDebug($"DidSendBodyData");
    //        var transfer = task.FromNative();
    //        this.onEvent.OnNext(transfer);
    //    }


    //    public override void DidWriteData(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long bytesWritten, long totalBytesWritten, long totalBytesExpectedToWrite)
    //    {
    //        this.logger.LogDebug("DidWriteData");
    //        this.onEvent.OnNext(downloadTask.FromNative());
    //    }


    //    public override void DidResume(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long resumeFileOffset, long expectedTotalBytes)
    //    {
    //        this.logger.LogDebug("DidResume");
    //        this.onEvent.OnNext(downloadTask.FromNative());
    //    }


    public override async void DidFinishDownloading(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, NSUrl location)
    {
        this.logger.LogDebug("DidFinishDownloading");
        var ht = this.transfers.FirstOrDefault(x => x.Identifier.Equals(downloadTask.TaskDescription));
        if (ht != null)
        {
            //if (!ht.LocalFilePath.IsEmpty())
            //    // if you are debugging, the base path tends to change, so the destination path changes too
            //    File.Copy(location.Path, transfer.LocalFilePath, true);

            await this.services.RunDelegates<IHttpTransferDelegate>(x => x.OnCompleted(ht));
        }
    }


    //protected virtual async Task HandleError(HttpTransfer transfer, NSError error)
    //{
    //    this.logger.LogError(transfer.Exception, "Error with HTTP transfer: " + transfer.Identifier);
    //    await this.shinyDelegates.Value.RunDelegates(
    //        x => x.OnError(transfer, transfer.Exception)
    //    );
    //}


    //    void TryDeleteUploadTempFile(HttpTransfer transfer)
    //    {
    //        if (!transfer.IsUpload)
    //            return;

    //        var path = this.platform.GetUploadTempFilePath(transfer);
    //        if (File.Exists(path))
    //        {
    //            try
    //            {
    //                this.logger.LogDebug($"Deleting temporary upload file - {transfer.Identifier}");

    //                // sometimes iOS will hold a file lock a bit longer than it should
    //                File.Delete(path);
    //                this.logger.LogDebug($"Temporary upload file deleted - {transfer.Identifier}");
    //            }
    //            catch (Exception ex)
    //            {
    //                this.logger.LogWarning($"Unable to delete temporary upload file - {transfer.Identifier}", ex);
    //            }
    //        }
    //    }
}