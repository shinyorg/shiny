using System;
using System.Reactive.Subjects;
using Foundation;


namespace Shiny.Net.Http
{
    public class CoreSessionDownloadDelegate : NSUrlSessionDownloadDelegate
    {
        readonly Subject<HttpTransfer> onEvent = new Subject<HttpTransfer>();
        internal IObservable<HttpTransfer> WhenEventOccurs => this.onEvent;


        // TODO: reload all transfer objects
        //public override void DidBecomeInvalid(NSUrlSession session, NSError error)
        //{
        //    // TODO: hmmm
        //}


        public override void DidCompleteWithError(NSUrlSession session, NSUrlSessionTask task, NSError error)
            => this.FireDelegate(task, (tdelegate, transfer) =>
            {
                var ex = new Exception(error.LocalizedDescription);
                transfer.Exception = ex;
                transfer.Status = HttpTransferState.Error;
                transfer.LastModified = DateTime.UtcNow;
                tdelegate.OnError(transfer, ex);
            });


        //public override void NeedNewBodyStream(NSUrlSession session, NSUrlSessionTask task, [BlockProxy(typeof(NIDActionArity1V0))] Action<NSInputStream> completionHandler)
        //{
        //}


        public override void DidSendBodyData(NSUrlSession session, NSUrlSessionTask task, long bytesSent, long totalBytesSent, long totalBytesExpectedToSend)
        {
            //var transfer = this.Get(task);
            // upload
        }


        public override void DidWriteData(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long bytesWritten, long totalBytesWritten, long totalBytesExpectedToWrite)
            => this.Set(downloadTask, transfer =>
            {
                transfer.Status = HttpTransferState.InProgress;
                transfer.BytesTransferred = totalBytesWritten;
                transfer.FileSize = totalBytesExpectedToWrite;
                //transfer.RunCalculations();
            });


        public override void DidResume(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long resumeFileOffset, long expectedTotalBytes)
            => this.Set(downloadTask, transfer => transfer.Status = HttpTransferState.InProgress);


        //public override void DidFinishCollectingMetrics(NSUrlSession session, NSUrlSessionTask task, NSUrlSessionTaskMetrics metrics)
        //{
        //    metrics.TaskInterval.
        //    //metrics.TransactionMetrics[0].
        //}


        public override void DidFinishDownloading(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, NSUrl location)
            => this.FireDelegate(downloadTask, (tdelegate, transfer) =>
            {
                transfer.Status = HttpTransferState.Completed;
                transfer.LastModified = DateTime.UtcNow;
                tdelegate.OnCompleted(transfer);
            });


        HttpTransfer Get(NSUrlSessionTask task)
        {
            return null;
        }


        void Set(NSUrlSessionTask task, Action<HttpTransfer> setter)
        {
            // TODO: set last modified
            var transfer = this.Get(task);
            setter(transfer);
            transfer.LastModified = DateTime.UtcNow;
        }


        void FireDelegate(NSUrlSessionTask task, Action<IHttpTransferDelegate, HttpTransfer> setter)
        {
            var transfer = this.Get(task);
            var del = ShinyHost.Resolve<IHttpTransferDelegate>();
            setter(del, transfer);
        }
    }
}
