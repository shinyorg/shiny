using System;
using System.IO;
using System.Reactive.Subjects;
using Foundation;
using Shiny.Logging;


namespace Shiny.Net.Http
{
    public class ShinyUrlSessionDelegate : NSUrlSessionDownloadDelegate
    {
        internal static Action CompletionHandler { get; set; }
        readonly IHttpTransferDelegate tdelegate;
        readonly Subject<HttpTransfer> onEvent;


        public ShinyUrlSessionDelegate(IHttpTransferDelegate tdelegate)
        {
            this.tdelegate = tdelegate;
            this.onEvent = new Subject<HttpTransfer>();
        }


        internal IObservable<HttpTransfer> WhenEventOccurs() => this.onEvent;


        // reauthorize?
        //public override void DidBecomeInvalid(NSUrlSession session, NSError error)
        //public override void NeedNewBodyStream(NSUrlSession session, NSUrlSessionTask task, Action<NSInputStream> completionHandler)
        //public override void DidFinishCollectingMetrics(NSUrlSession session, NSUrlSessionTask task, NSUrlSessionTaskMetrics metrics)


        public override void DidReceiveChallenge(NSUrlSession session, NSUrlAuthenticationChallenge challenge, Action<NSUrlSessionAuthChallengeDisposition, NSUrlCredential> completionHandler)
            => completionHandler.Invoke(NSUrlSessionAuthChallengeDisposition.PerformDefaultHandling, null);


        public override void DidReceiveChallenge(NSUrlSession session, NSUrlSessionTask task, NSUrlAuthenticationChallenge challenge, Action<NSUrlSessionAuthChallengeDisposition, NSUrlCredential> completionHandler)
            => completionHandler.Invoke(NSUrlSessionAuthChallengeDisposition.PerformDefaultHandling, null);


        public override void DidFinishEventsForBackgroundSession(NSUrlSession session)
            => CompletionHandler?.Invoke();


        public override void DidCompleteWithError(NSUrlSession session, NSUrlSessionTask task, NSError error)
        {
            var transfer = task.FromNative();
            Log.Write(transfer.Exception, ("HttpTransfer", transfer.Identifier));
            this.tdelegate.OnError(transfer, transfer.Exception);
            this.onEvent.OnNext(transfer);
        }


        public override void DidSendBodyData(NSUrlSession session, NSUrlSessionTask task, long bytesSent, long totalBytesSent, long totalBytesExpectedToSend)
            => this.onEvent.OnNext(task.FromNative());


        public override void DidWriteData(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long bytesWritten, long totalBytesWritten, long totalBytesExpectedToWrite)
            => this.onEvent.OnNext(downloadTask.FromNative());


        public override void DidResume(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long resumeFileOffset, long expectedTotalBytes)
            => this.onEvent.OnNext(downloadTask.FromNative());


        public override void DidFinishDownloading(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, NSUrl location)
        {
            var transfer = downloadTask.FromNative();
            if (!transfer.LocalFilePath.IsEmpty())
                File.Move(location.Path, transfer.LocalFilePath);

            this.tdelegate.OnCompleted(transfer);
            this.onEvent.OnNext(transfer);
        }
    }
}
