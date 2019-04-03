using System;
using Foundation;
using ObjCRuntime;


namespace Shiny.Net.Http
{
    public class CoreSessionDownloadDelegate : NSUrlSessionDownloadDelegate
    {
        // TODO: reload all transfer objects
        public override void DidBecomeInvalid(NSUrlSession session, NSError error)
        {
            // TODO: hmmm
        }


        public override void DidCompleteWithError(NSUrlSession session, NSUrlSessionTask task, NSError error)
        {
        }


        //public override void NeedNewBodyStream(NSUrlSession session, NSUrlSessionTask task, [BlockProxy(typeof(NIDActionArity1V0))] Action<NSInputStream> completionHandler)
        //{
        //}

        // upload
        public override void DidSendBodyData(NSUrlSession session, NSUrlSessionTask task, long bytesSent, long totalBytesSent, long totalBytesExpectedToSend)
        {
            var transfer = this.Get(task);
            // upload
        }


        public override void DidWriteData(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long bytesWritten, long totalBytesWritten, long totalBytesExpectedToWrite)
        {
            var transfer = this.Get(downloadTask);

        }


        public override void DidResume(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long resumeFileOffset, long expectedTotalBytes)
        {
            var transfer = this.Get(downloadTask);
            transfer.Status = HttpTransferState.Resumed; // TODO: or just running?
            transfer.ResumeOffset = resumeFileOffset;
        }


        //public override void DidFinishCollectingMetrics(NSUrlSession session, NSUrlSessionTask task, NSUrlSessionTaskMetrics metrics)
        //{
        //    //metrics.TaskInterval
        //    //metrics.TransactionMetrics[0].
        //}


        public override void DidFinishDownloading(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, NSUrl location)
        {
            var transfer = this.Get(downloadTask);
            transfer.Status = HttpTransferState.Completed;
            transfer.PercentComplete = 100;
            transfer.BytesTransferred = transfer.FileSize;
        }


        HttpTransfer Get(NSUrlSessionTask task)
        {
            return null;
        }
    }
}
