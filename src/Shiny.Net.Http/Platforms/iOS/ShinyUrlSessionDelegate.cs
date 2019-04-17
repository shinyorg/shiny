using System;
using System.IO;
using System.Reactive.Subjects;
using Foundation;
using Shiny.Logging;


namespace Shiny.Net.Http
{
    //public class ShinyUrlSessionDelegate : NSObject, INSUrlSessionDownloadDelegate
    public class ShinyUrlSessionDelegate : NSUrlSessionDownloadDelegate
    {
        internal static Action CompletionHandler { get; set; }
        readonly IHttpTransferDelegate tdelegate;
        readonly Subject<HttpTransfer> onEvent;
        readonly HttpTransferManager manager;


        public ShinyUrlSessionDelegate(HttpTransferManager manager, IHttpTransferDelegate tdelegate)
        {
            this.manager = manager;
            this.tdelegate = tdelegate;
            this.onEvent = new Subject<HttpTransfer>();
        }


        internal IObservable<HttpTransfer> WhenEventOccurs() => this.onEvent;


        public override void DidBecomeInvalid(NSUrlSession session, NSError error)
        {
            this.manager.Session = null;
            if (error != null)
                Log.Write(new Exception(error.LocalizedDescription));
        }


        // reauthorize?
        //public override void NeedNewBodyStream(NSUrlSession session, NSUrlSessionTask task, Action<NSInputStream> completionHandler)


        public override void DidReceiveChallenge(NSUrlSession session, NSUrlAuthenticationChallenge challenge, Action<NSUrlSessionAuthChallengeDisposition, NSUrlCredential> completionHandler)
            => completionHandler.Invoke(NSUrlSessionAuthChallengeDisposition.PerformDefaultHandling, null);


        public override void DidReceiveChallenge(NSUrlSession session, NSUrlSessionTask task, NSUrlAuthenticationChallenge challenge, Action<NSUrlSessionAuthChallengeDisposition, NSUrlCredential> completionHandler)
            => completionHandler.Invoke(NSUrlSessionAuthChallengeDisposition.PerformDefaultHandling, null);


        public override void DidFinishEventsForBackgroundSession(NSUrlSession session)
        {
            this.manager.Session = null;
            CompletionHandler?.Invoke();
        }


        public override void DidCompleteWithError(NSUrlSession session, NSUrlSessionTask task, NSError error)
        {
            var transfer = task.FromNative();

            if (task.State != NSUrlSessionTaskState.Canceling && error != null)
            {
                Log.Write(transfer.Exception, ("HttpTransfer", transfer.Identifier));
                this.tdelegate.OnError(transfer, transfer.Exception);
            }
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
                // if you are debugging, the base path tends to change, so the destination path changes too
                File.Copy(location.Path, transfer.LocalFilePath, true);

            this.tdelegate.OnCompleted(transfer);
            this.onEvent.OnNext(transfer);
        }
    }
}
