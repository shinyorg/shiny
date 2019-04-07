using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Foundation;
using Shiny.Infrastructure;
using Shiny.Net.Http.Infrastructure;


namespace Shiny.Net.Http
{
    public class HttpTransferManager : AbstractHttpTransferManager
    {
        readonly ShinyUrlSessionDelegate sessionDelegate;
        readonly NSUrlSessionConfiguration sessionConfig;
        readonly NSUrlSession session;


        public HttpTransferManager(IRepository repository,
                                   IHttpTransferDelegate httpDelegate,
                                   int maxConnectionsPerHost = 1)
        {
            this.sessionDelegate = new ShinyUrlSessionDelegate(repository, httpDelegate);
            this.sessionConfig = NSUrlSessionConfiguration.CreateBackgroundSessionConfiguration(NSBundle.MainBundle.BundleIdentifier + ".BackgroundTransferSession");
            this.sessionConfig.HttpMaximumConnectionsPerHost = maxConnectionsPerHost;

            this.session = NSUrlSession.FromConfiguration(
                this.sessionConfig,
                this.sessionDelegate,
                new NSOperationQueue()
            );
        }


        public override async Task Cancel(IHttpTransfer transfer)
        {
            var t = (HttpTransfer)transfer;
            //if (t.Status == HttpTransferState.Running)

            if (t.UploadTask == null)
                t.DownloadTask.Cancel();
            else
                t.UploadTask.Cancel();

            t.Status = HttpTransferState.Cancelled;
            await this.sessionDelegate.Remove(t);
        }


        protected override async Task<IHttpTransfer> CreateDownload(HttpTransferRequest request)
        {
            var task = this.session.CreateDownloadTask(request.ToNative());
            var transfer = new HttpTransfer(task, request);
            await this.sessionDelegate.Add(transfer);
            task.Resume();

            return transfer;
        }


        protected override async Task<IHttpTransfer> CreateUpload(HttpTransferRequest request)
        {
            var task = this.session.CreateUploadTask(request.ToNative());
            var transfer = new HttpTransfer(task, request);
            await this.sessionDelegate.Add(transfer);
            task.Resume();

            return transfer;
        }


        public override IObservable<IHttpTransfer> WhenUpdated()
            => this.sessionDelegate.WhenEventOccurs();


        protected override Task<IEnumerable<IHttpTransfer>> GetUploads(QueryFilter filter)
        {
            var results = this.sessionDelegate
                .GetCurrentTransfers()
                .Where(x => x.UploadTask != null)
                .Cast<IHttpTransfer>();

            return Task.FromResult(results);
        }


        protected override Task<IEnumerable<IHttpTransfer>> GetDownloads(QueryFilter filter)
        {
            var results = this.sessionDelegate
                .GetCurrentTransfers()
                .Where(x => x.DownloadTask != null)
                .Cast<IHttpTransfer>();

            return Task.FromResult(results);
        }
    }
}