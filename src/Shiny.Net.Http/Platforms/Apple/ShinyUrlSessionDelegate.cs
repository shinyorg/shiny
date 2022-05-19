using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using CoreFoundation;
using Foundation;
using Microsoft.Extensions.Logging;

namespace Shiny.Net.Http;

public class ShinyUrlSessionDelegate : NSUrlSessionDownloadDelegate
{
    internal static Action? CompletionHandler { get; set; }
    readonly Lazy<IEnumerable<IHttpTransferDelegate>> shinyDelegates;
    readonly Subject<HttpTransfer> onEvent = new();
    readonly HttpTransferManager manager;
    readonly ILogger logger;
    readonly IPlatform platform;


    public ShinyUrlSessionDelegate(
        HttpTransferManager manager,
        IServiceProvider serviceProvider,
        ILogger logger,
        IPlatform platform
    )
    {
        this.manager = manager;
        this.logger = logger;
        this.platform = platform;
        this.shinyDelegates = serviceProvider.GetLazyService<IEnumerable<IHttpTransferDelegate>>();
    }


    internal IObservable<HttpTransfer> WhenEventOccurs() => this.onEvent;


    public override void DidBecomeInvalid(NSUrlSession session, NSError error)
    {
        this.logger.LogDebug($"DidBecomeInvalid");
        this.manager.CompleteSession();
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
        this.manager.CompleteSession();
        CompletionHandler?.Invoke();
    }


    public override async void DidCompleteWithError(NSUrlSession session, NSUrlSessionTask task, NSError error)
    {
        this.logger.LogDebug($"DidCompleteWithError");
        var transfer = task.FromNative();

        switch (transfer.Status)
        {
            case HttpTransferState.Canceled:
                this.logger.LogWarning($"Transfer {transfer.Identifier} was cancelled");
                break;

            case HttpTransferState.Completed:
                this.logger.LogInformation($"Transfer {transfer.Identifier} completed successfully");
                await this.shinyDelegates.Value.RunDelegates(
                    x => x.OnCompleted(transfer)
                );
                break;

            default:
                await this.HandleError(transfer, error);
                break;
        }
        this.TryDeleteUploadTempFile(transfer);
        this.onEvent.OnNext(transfer);
    }


    public override void DidSendBodyData(NSUrlSession session, NSUrlSessionTask task, long bytesSent, long totalBytesSent, long totalBytesExpectedToSend)
    {
        this.logger.LogDebug($"DidSendBodyData");
        var transfer = task.FromNative();
        this.onEvent.OnNext(transfer);
    }


    public override void DidWriteData(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long bytesWritten, long totalBytesWritten, long totalBytesExpectedToWrite)
    {
        this.logger.LogDebug("DidWriteData");
        this.onEvent.OnNext(downloadTask.FromNative());
    }


    public override void DidResume(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long resumeFileOffset, long expectedTotalBytes)
    {
        this.logger.LogDebug("DidResume");
        this.onEvent.OnNext(downloadTask.FromNative());
    }


    public override async void DidFinishDownloading(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, NSUrl location)
    {
        this.logger.LogDebug("DidFinishDownloading");
        var transfer = downloadTask.FromNative();

        if (!transfer.LocalFilePath.IsEmpty())
            // if you are debugging, the base path tends to change, so the destination path changes too
            File.Copy(location.Path, transfer.LocalFilePath, true);

        await this.shinyDelegates.Value.RunDelegates(x => x.OnCompleted(transfer));
        this.onEvent.OnNext(transfer);
    }


    protected virtual async Task HandleError(HttpTransfer transfer, NSError error)
    {
        this.logger.LogError(transfer.Exception, "Error with HTTP transfer: " + transfer.Identifier);
        await this.shinyDelegates.Value.RunDelegates(
            x => x.OnError(transfer, transfer.Exception)
        );
    }


    void TryDeleteUploadTempFile(HttpTransfer transfer)
    {
        if (!transfer.IsUpload)
            return;

        var path = this.platform.GetUploadTempFilePath(transfer);
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
