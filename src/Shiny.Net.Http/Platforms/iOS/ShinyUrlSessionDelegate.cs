using System;
using System.Collections.Generic;
using System.IO;
using System.Reactive.Subjects;
using Foundation;
using Microsoft.Extensions.Logging;


namespace Shiny.Net.Http
{
    public class ShinyUrlSessionDelegate : NSUrlSessionDownloadDelegate
    {
        internal static Action? CompletionHandler { get; set; }
        readonly Lazy<IEnumerable<IHttpTransferDelegate>> shinyDelegates = ShinyHost.LazyResolve<IEnumerable<IHttpTransferDelegate>>();
        readonly Subject<HttpTransfer> onEvent;
        readonly HttpTransferManager manager;
        readonly ILogger logger;


        public ShinyUrlSessionDelegate(HttpTransferManager manager, ILogger logger)
        {
            this.manager = manager;
            this.logger = logger;
            this.onEvent = new Subject<HttpTransfer>();
        }


        internal IObservable<HttpTransfer> WhenEventOccurs() => this.onEvent;


        public override void DidBecomeInvalid(NSUrlSession session, NSError error)
        {
            this.manager.CompleteSession();
            if (error != null)
                this.logger.LogError(new Exception(error.LocalizedDescription), "DidBecomeInvalid reported an error");
        }


        // reauthorize?
        public override void NeedNewBodyStream(NSUrlSession session, NSUrlSessionTask task, Action<NSInputStream> completionHandler)
        {
            var transfer = task.FromNative();
            var file = new FileInfo(transfer.LocalFilePath);
            var stream = new BodyStream(file);
            stream.Open();
            completionHandler(stream);
        }


        public override void DidReceiveChallenge(NSUrlSession session, NSUrlAuthenticationChallenge challenge, Action<NSUrlSessionAuthChallengeDisposition, NSUrlCredential> completionHandler)
            => completionHandler.Invoke(NSUrlSessionAuthChallengeDisposition.PerformDefaultHandling, null);

        public override void DidReceiveChallenge(NSUrlSession session, NSUrlSessionTask task, NSUrlAuthenticationChallenge challenge, Action<NSUrlSessionAuthChallengeDisposition, NSUrlCredential> completionHandler)
            => completionHandler.Invoke(NSUrlSessionAuthChallengeDisposition.PerformDefaultHandling, null);


        public override void DidFinishEventsForBackgroundSession(NSUrlSession session)
        {
            this.manager.CompleteSession();
            CompletionHandler?.Invoke();
        }


        public override async void DidCompleteWithError(NSUrlSession session, NSUrlSessionTask task, NSError error)
        {
            var transfer = task.FromNative();

            if (task.State != NSUrlSessionTaskState.Canceling && error != null && transfer.Exception != null)
            {
                this.logger.LogError(transfer.Exception, "Error with HTTP transfer: " + transfer.Identifier);
                await this.shinyDelegates.Value.RunDelegates(
                    x => x.OnError(transfer, transfer.Exception)
                );
            }
            this.onEvent.OnNext(transfer);
        }


        public override async void DidSendBodyData(NSUrlSession session, NSUrlSessionTask task, long bytesSent, long totalBytesSent, long totalBytesExpectedToSend)
        {
            var transfer = task.FromNative();
            if (transfer.PercentComplete >= 1.0)
            {
                await this.shinyDelegates.Value.RunDelegates<IHttpTransferDelegate>(
                    x => x.OnCompleted(transfer)
                );
            }
            this.onEvent.OnNext(transfer);
        }


        public override void DidWriteData(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long bytesWritten, long totalBytesWritten, long totalBytesExpectedToWrite)
            => this.onEvent.OnNext(downloadTask.FromNative());


        public override void DidResume(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long resumeFileOffset, long expectedTotalBytes)
            => this.onEvent.OnNext(downloadTask.FromNative());


        public override async void DidFinishDownloading(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, NSUrl location)
        {
            var transfer = downloadTask.FromNative();

            if (!transfer.LocalFilePath.IsEmpty())
                // if you are debugging, the base path tends to change, so the destination path changes too
                File.Copy(location.Path, transfer.LocalFilePath, true);

            await this.shinyDelegates.Value.RunDelegates(x => x.OnCompleted(transfer));
            this.onEvent.OnNext(transfer);
        }
    }
}
