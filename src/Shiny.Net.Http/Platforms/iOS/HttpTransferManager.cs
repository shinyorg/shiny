using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Foundation;
using Shiny.Infrastructure;


namespace Shiny.Net.Http
{
    public class HttpTransferManager : AbstractHttpTransferManager
    {
        readonly IRepository repository;
        readonly CoreSessionDownloadDelegate sessionDelegate;
        readonly NSUrlSessionConfiguration sessionConfig;
        readonly NSUrlSession session;


        public HttpTransferManager(IRepository repository, int maxConnectionsPerHost = 1)
        {
            this.repository = repository;
            this.sessionDelegate = new CoreSessionDownloadDelegate();
            this.sessionConfig = NSUrlSessionConfiguration.CreateBackgroundSessionConfiguration(NSBundle.MainBundle.BundleIdentifier + ".BackgroundTransferSession");
            this.sessionConfig.HttpMaximumConnectionsPerHost = maxConnectionsPerHost;

            this.session = NSUrlSession.FromConfiguration(
                this.sessionConfig,
                this.sessionDelegate,
                new NSOperationQueue()
            );
        }


        public override Task Cancel(IHttpTransfer transfer)
        {
            var t = (HttpTransfer)transfer;
            //if (t.Status == HttpTransferState.Running)

            if (t.UploadTask == null)
                t.DownloadTask.Cancel();
            else
                t.UploadTask.Cancel();

            t.Status = HttpTransferState.Cancelled;
            return Task.CompletedTask;
        }


        protected override Task<IHttpTransfer> CreateDownload(HttpTransferRequest request)
        {
            var task = this.session.CreateDownloadTask(request.ToNative());
            var transfer = new HttpTransfer(task, request);

            return Task.FromResult<IHttpTransfer>(transfer);
        }


        protected override Task<IHttpTransfer> CreateUpload(HttpTransferRequest request)
        {
            var task = this.session.CreateUploadTask(request.ToNative());
            var transfer = new HttpTransfer(task, request);

            return Task.FromResult<IHttpTransfer>(transfer);
        }


        public override IObservable<IHttpTransfer> WhenUpdated()
            => this.sessionDelegate.WhenEventOccurs();


        protected override async Task<IEnumerable<IHttpTransfer>> GetUploads(QueryFilter filter)
        {
            var tasks = await this.session.GetAllTasksAsync();
            return tasks
                .OfType<NSUrlSessionUploadTask>()
                .Select(x => new HttpTransfer(x, null));
            // TODO: resume?
        }


        protected override async Task<IEnumerable<IHttpTransfer>> GetDownloads(QueryFilter filter)
        {
            var tasks = await this.session.GetAllTasksAsync();
            return tasks
                .OfType<NSUrlSessionDownloadTask>()
                .Select(x => new HttpTransfer(x, null));
            // TODO: resume?
        }
    }
}