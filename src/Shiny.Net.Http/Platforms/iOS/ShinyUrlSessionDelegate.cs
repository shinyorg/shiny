using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reactive.Subjects;
using Foundation;
using Shiny.Infrastructure;
using Shiny.Logging;


namespace Shiny.Net.Http
{
    public class ShinyUrlSessionDelegate : NSUrlSessionDownloadDelegate
    {
        readonly IRepository repository;
        readonly IHttpTransferDelegate tdelegate;

        readonly Subject<IHttpTransfer> onEvent;
        readonly object syncLock;
        readonly IDictionary<nuint, HttpTransfer> currentTransfers;


        public ShinyUrlSessionDelegate(IRepository repository, IHttpTransferDelegate tdelegate)
        {
            this.repository = repository;
            this.tdelegate = tdelegate;

            this.syncLock = new object();
            this.onEvent = new Subject<IHttpTransfer>();
            this.currentTransfers = new Dictionary<nuint, HttpTransfer>();
            // TODO: I need to reload the dictionary with current tasks and get all of the repo data (repo first likely) - likely need to restart task as well
        }


        internal async void Init(NSUrlSession session)
        {
            try
            {
                var tasks = await session.GetAllTasksAsync();
                lock (this.syncLock)
                {
                    //var transfers = await this.repository.GetAll<object>();
                    foreach (var task in tasks)
                    {
                        var transfer = task is NSUrlSessionDownloadTask dl
                            ? new HttpTransfer(dl, null)
                            : new HttpTransfer((NSUrlSessionUploadTask)task, null);

                        this.currentTransfers.Add(transfer.NativeIdentifier, transfer);
                        task.Resume();
                    }
                }
            }
            catch (Exception ex)
            {
                Log.Write(ex);
            }
        }


        internal IObservable<IHttpTransfer> WhenEventOccurs() => this.onEvent;
        internal IEnumerable<HttpTransfer> GetCurrentTransfers()
        {
            lock (this.syncLock)
                return this.currentTransfers.Values.ToList();
        }


        public void Add(HttpTransfer transfer)
        {
            // TODO: add repo data
            lock (this.syncLock)
                this.currentTransfers.Add(transfer.NativeIdentifier, transfer);
        }


        public void Remove(HttpTransfer transfer)
        {
            // TODO: kill repo data
            lock (this.syncLock)
                this.currentTransfers.Remove(transfer.NativeIdentifier);
        }


        void Set(NSUrlSessionTask task, Action<HttpTransfer> setter)
        {
            HttpTransfer transfer = null;
            lock (this.syncLock)
            {
                transfer = this.currentTransfers[task.TaskIdentifier];
                setter(transfer);
                transfer.LastModified = DateTime.UtcNow;
                switch (transfer.Status)
                {
                    case HttpTransferState.Cancelled:
                    case HttpTransferState.Error:
                    case HttpTransferState.Completed:
                        // TODO: repo data
                        this.currentTransfers.Remove(transfer.NativeIdentifier);
                        break;
                }
            }
            if (this.onEvent.HasObservers)
            {
                // TODO: calc metrics? need to compare last vs current transfer
                this.onEvent.OnNext(transfer);
            }
        }

        // TODO: reload all transfer objects
        //public override void DidBecomeInvalid(NSUrlSession session, NSError error)
        //{
        //    // TODO: hmmm
        //}


        public override void DidCompleteWithError(NSUrlSession session, NSUrlSessionTask task, NSError error)
            => this.Set(task, transfer =>
            {
                var ex = new Exception(error.LocalizedDescription);
                transfer.Exception = ex;
                transfer.Status = HttpTransferState.Error;
                transfer.LastModified = DateTime.UtcNow;
                Log.Write(ex, ("HttpTransfer", transfer.Identifier));
                this.tdelegate.OnError(transfer, ex);
            });


        //public override void NeedNewBodyStream(NSUrlSession session, NSUrlSessionTask task, [BlockProxy(typeof(NIDActionArity1V0))] Action<NSInputStream> completionHandler)
        //{
        //}


        public override void DidSendBodyData(NSUrlSession session, NSUrlSessionTask task, long bytesSent, long totalBytesSent, long totalBytesExpectedToSend)
            => this.Set(task, transfer =>
            {
                transfer.Status = HttpTransferState.InProgress;
                transfer.BytesTransferred = totalBytesSent;
            });


        public override void DidWriteData(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long bytesWritten, long totalBytesWritten, long totalBytesExpectedToWrite)
            => this.Set(downloadTask, transfer =>
            {
                transfer.Status = HttpTransferState.InProgress;
                transfer.BytesTransferred = totalBytesWritten;
                transfer.FileSize = totalBytesExpectedToWrite;
            });


        public override void DidResume(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long resumeFileOffset, long expectedTotalBytes)
            => this.Set(downloadTask, transfer => transfer.Status = HttpTransferState.InProgress);


        //public override void DidFinishCollectingMetrics(NSUrlSession session, NSUrlSessionTask task, NSUrlSessionTaskMetrics metrics)
        //{
        //    metrics.TaskInterval.
        //    //metrics.TransactionMetrics[0].
        //}


        public override void DidFinishDownloading(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, NSUrl location)
            => this.Set(downloadTask, transfer =>
            {
                transfer.Status = HttpTransferState.Completed;
                transfer.LastModified = DateTime.UtcNow;
                File.Move(location.Path, transfer.Request.LocalFile.FullName);
                this.tdelegate.OnCompleted(transfer);
            });
    }
}
