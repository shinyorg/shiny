using System;
using Foundation;
using ObjCRuntime;


namespace Shiny.Net.Http
{
    public class CoreSessionDownloadDelegate : NSUrlSessionDownloadDelegate
    {
        public override void DidBecomeInvalid(NSUrlSession session, NSError error)
        {
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
        }


        // download
        public override void DidWriteData(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long bytesWritten, long totalBytesWritten, long totalBytesExpectedToWrite)
        {
        }


        public override void DidResume(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long resumeFileOffset, long expectedTotalBytes)
        {
        }


        public override void DidFinishCollectingMetrics(NSUrlSession session, NSUrlSessionTask task, NSUrlSessionTaskMetrics metrics)
        {
            //metrics.TaskInterval
            //metrics.TransactionMetrics[0].
        }


        public override void DidFinishDownloading(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, NSUrl location)
        {
        }
    }
}
