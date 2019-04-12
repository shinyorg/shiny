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
            this.sessionConfig = NSUrlSessionConfiguration.CreateBackgroundSessionConfiguration(SessionName);
            this.sessionConfig.HttpMaximumConnectionsPerHost = maxConnectionsPerHost;

            this.session = NSUrlSession.FromConfiguration(
                this.sessionConfig,
                this.sessionDelegate,
                new NSOperationQueue()
            );
        }


        static string SessionName => $"{NSBundle.MainBundle.BundleIdentifier}.BackgroundTransferSession";
        public static void SetCompletionHandler(string sessionName, Action completionHandler)
        {
            if (SessionName.Equals(sessionName))
                ShinyUrlSessionDelegate.CompletionHandler = completionHandler;
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
            var transfer = await this.sessionDelegate.Add(() =>
            {
                var task = this.session.CreateDownloadTask(request.ToNative());
                return new HttpTransfer(task, request);
            });
            transfer.DownloadTask.Resume();
            return transfer;
        }


        protected override async Task<IHttpTransfer> CreateUpload(HttpTransferRequest request)
        {
            var transfer = await this.sessionDelegate.Add(() =>
            {
                var task = this.session.CreateUploadTask(request.ToNative());
                return new HttpTransfer(task, request);
            });
            transfer.UploadTask.Resume();

            return transfer;
        }


        public override IObservable<IHttpTransfer> WhenUpdated()
            => this.sessionDelegate.WhenEventOccurs();


        protected override async Task<IEnumerable<IHttpTransfer>> GetUploads(QueryFilter filter)
        {
            await this.sessionDelegate.Init(this.session);
            var results = this.sessionDelegate
                .GetCurrentTransfers()
                .Where(x => x.UploadTask != null)
                .Cast<IHttpTransfer>();

            results = Filter(results, filter);
            return results;
        }


        protected override async Task<IEnumerable<IHttpTransfer>> GetDownloads(QueryFilter filter)
        {
            await this.sessionDelegate.Init(this.session);
            var results = this.sessionDelegate
                .GetCurrentTransfers()
                .Where(x => x.DownloadTask != null)
                .Cast<IHttpTransfer>();

            results = Filter(results, filter);
            return results;
        }


        static IEnumerable<IHttpTransfer> Filter(IEnumerable<IHttpTransfer> current, QueryFilter filter)
        {
            if (filter.Ids.Any())
                current = current.Where(x => filter.Ids.Any(y => y == x.Identifier));

            switch (filter.States)
            {
                case HttpTransferStateFilter.InProgress:
                    current = current.Where(x => x.Status == HttpTransferState.InProgress);
                    break;

                case HttpTransferStateFilter.Pending:
                    current = current.Where(x => x.Status == HttpTransferState.Pending);
                    break;

                    // TODO: paused
            }
            return current;
        }
    }
}