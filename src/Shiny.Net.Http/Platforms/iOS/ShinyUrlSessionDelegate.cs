using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Net.Http;
using System.Reactive.Subjects;
using System.Threading.Tasks;
using Foundation;
using Shiny.Infrastructure;
using Shiny.Logging;
using Shiny.Net.Http.Infrastructure;


namespace Shiny.Net.Http
{
    public class ShinyUrlSessionDelegate : NSUrlSessionDownloadDelegate
    {
        internal static Action CompletionHandler { get; set; }
        readonly IRepository repository;
        readonly IHttpTransferDelegate tdelegate;

        readonly Subject<IHttpTransfer> onEvent;
        readonly object syncLock;
        IDictionary<nuint, HttpTransfer> currentTransfers;


        public ShinyUrlSessionDelegate(IRepository repository, IHttpTransferDelegate tdelegate)
        {
            this.repository = repository;
            this.tdelegate = tdelegate;

            this.syncLock = new object();
            this.onEvent = new Subject<IHttpTransfer>();
        }



        async Task Init(NSUrlSession session)
        {
            if (this.currentTransfers == null)
            {
                try
                {
                    var tasks = await session.GetAllTasksAsync();
                    var transfers = await this.repository.GetAll<HttpTransferStore>();

                    lock (this.syncLock)
                    {
                        if (this.currentTransfers == null)
                        {
                            this.currentTransfers = new Dictionary<nuint, HttpTransfer>();
                            foreach (var task in tasks)
                            {
                                var id = task.TaskIdentifier.ToString();
                                var store = transfers.FirstOrDefault(x => x.Identifier == id);
                                var request = FromStore(store);

                                var transfer = task is NSUrlSessionDownloadTask dl
                                    ? new HttpTransfer(dl, request)
                                    : new HttpTransfer((NSUrlSessionUploadTask)task, request);

                                this.currentTransfers.Add(transfer.NativeIdentifier, transfer);
                            }
                        }
                    }
                    //foreach (var task in tasks)
                    //    task.Resume();
                }
                catch (Exception ex)
                {
                    Log.Write(ex);
                }
            }
        }


        internal IObservable<IHttpTransfer> WhenEventOccurs() => this.onEvent;
        internal IEnumerable<HttpTransfer> GetCurrentTransfers()
        {
            lock (this.syncLock)
                return this.currentTransfers.Values.ToList();
        }


        public async Task<HttpTransfer> Add(Func<HttpTransfer> createTransfer)
        {
            HttpTransfer transfer = null;
            lock (this.syncLock)
            {
                transfer = createTransfer();
                this.currentTransfers.Add(transfer.NativeIdentifier, transfer);
            }

            await this.repository.Set(transfer.Identifier, new HttpTransferStore
            {
                Identifier = transfer.Identifier,
                Uri = transfer.Request.Uri,
                LocalFilePath = transfer.Request.LocalFile.FullName,
                UseMeteredConnection = transfer.Request.UseMeteredConnection,
                PostData = transfer.Request.PostData,
                Description = transfer.Request.Description,
                IsUpload = transfer.Request.IsUpload,
                HttpMethod = transfer.Request.HttpMethod.Method,
                Headers = transfer.Request.Headers
            });
            return transfer;
        }


        public async Task Remove(HttpTransfer transfer)
        {
            lock (this.syncLock)
                this.currentTransfers.Remove(transfer.NativeIdentifier);

            await this.repository.Remove<HttpTransferStore>(transfer.Identifier);
        }


        async void Set(NSUrlSession session, NSUrlSessionTask task, Action<HttpTransfer> setter)
        {
            await this.Init(session);

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
                        this.currentTransfers.Remove(transfer.NativeIdentifier);
                        this.repository.Remove<HttpTransferStore>(transfer.Identifier); // fire and forget
                        break;
                }
            }
            this.onEvent.OnNext(transfer);
        }


        // reauthorize?
        //public override void DidBecomeInvalid(NSUrlSession session, NSError error)
        //public override void NeedNewBodyStream(NSUrlSession session, NSUrlSessionTask task, [BlockProxy(typeof(NIDActionArity1V0))] Action<NSInputStream> completionHandler)
        //public override void DidFinishCollectingMetrics(NSUrlSession session, NSUrlSessionTask task, NSUrlSessionTaskMetrics metrics)



        public override void DidReceiveChallenge(NSUrlSession session, NSUrlAuthenticationChallenge challenge, Action<NSUrlSessionAuthChallengeDisposition, NSUrlCredential> completionHandler)
            => completionHandler.Invoke(NSUrlSessionAuthChallengeDisposition.PerformDefaultHandling, null);


        public override void DidReceiveChallenge(NSUrlSession session, NSUrlSessionTask task, NSUrlAuthenticationChallenge challenge, Action<NSUrlSessionAuthChallengeDisposition, NSUrlCredential> completionHandler)
            => completionHandler.Invoke(NSUrlSessionAuthChallengeDisposition.PerformDefaultHandling, null);


        public override void DidFinishEventsForBackgroundSession(NSUrlSession session)
            => CompletionHandler?.Invoke();


        public override void DidCompleteWithError(NSUrlSession session, NSUrlSessionTask task, NSError error)
            => this.Set(session, task, transfer =>
            {
                var ex = new Exception(error.LocalizedDescription);
                transfer.Exception = ex;
                transfer.Status = HttpTransferState.Error;
                transfer.LastModified = DateTime.UtcNow;
                Log.Write(ex, ("HttpTransfer", transfer.Identifier));
                this.tdelegate.OnError(transfer, ex);
            });


        public override void DidSendBodyData(NSUrlSession session, NSUrlSessionTask task, long bytesSent, long totalBytesSent, long totalBytesExpectedToSend)
            => this.Set(session, task, transfer =>
            {
                transfer.Status = HttpTransferState.InProgress;
                transfer.BytesTransferred = totalBytesSent;
            });


        public override void DidWriteData(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long bytesWritten, long totalBytesWritten, long totalBytesExpectedToWrite)
            => this.Set(session, downloadTask, transfer =>
            {
                transfer.Status = HttpTransferState.InProgress;
                transfer.BytesTransferred = totalBytesWritten;
                transfer.FileSize = totalBytesExpectedToWrite;
            });


        public override void DidResume(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, long resumeFileOffset, long expectedTotalBytes)
            => this.Set(session, downloadTask, transfer => transfer.Status = HttpTransferState.InProgress);


        public override void DidFinishDownloading(NSUrlSession session, NSUrlSessionDownloadTask downloadTask, NSUrl location)
            => this.Set(session, downloadTask, transfer =>
            {
                transfer.Status = HttpTransferState.Completed;
                transfer.LastModified = DateTime.UtcNow;
                File.Move(location.Path, transfer.Request.LocalFile.FullName);

                this.tdelegate.OnCompleted(transfer);
            });


        static HttpTransferRequest FromStore(HttpTransferStore store) =>
            new HttpTransferRequest(
                store.Uri,
                new FileInfo(store.LocalFilePath),
                store.IsUpload
            )
            {
                UseMeteredConnection = store.UseMeteredConnection,
                PostData = store.PostData,
                HttpMethod = new HttpMethod(store.HttpMethod),
                Description = store.Description,
                Headers = store.Headers
            };
    }
}
