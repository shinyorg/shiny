using System;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;
using Foundation;
using Shiny.Net.Http.Infrastructure;


namespace Shiny.Net.Http
{
    public class HttpTransferManager : AbstractHttpTransferManager
    {
        readonly ShinyUrlSessionDelegate sessionDelegate;
        readonly NSUrlSessionConfiguration sessionConfig;
        readonly NSUrlSession session;


        public HttpTransferManager(IHttpTransferDelegate httpDelegate,
                                   int maxConnectionsPerHost = 1)
        {
            this.sessionDelegate = new ShinyUrlSessionDelegate(httpDelegate);
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


        protected override Task<HttpTransfer> CreateDownload(HttpTransferRequest request)
        {
            var task = this.session.CreateDownloadTask(request.ToNative());
            var transfer = task.FromNative();
            task.Resume();

            return Task.FromResult(transfer);
        }


        protected override Task<HttpTransfer> CreateUpload(HttpTransferRequest request)
        {
            var task = this.session.CreateUploadTask(request.ToNative());
            var transfer = task.FromNative();
            task.Resume();

            return Task.FromResult(transfer);
        }


        public override IObservable<HttpTransfer> WhenUpdated()
            => this.sessionDelegate.WhenEventOccurs();


        public override Task<IEnumerable<HttpTransfer>> GetTransfers(QueryFilter filter = null)
            => this.session.QueryTransfers(filter);


        public override async Task Cancel(QueryFilter filter = null)
        {
            var tasks = await this.session.QueryTasks(filter);
            foreach (var task in tasks)
                task.Cancel();
        }


        public override async Task Cancel(string id)
        {
            var taskId = nuint.Parse(id);
            var tasks = await this.session.GetAllTasksAsync();
            var task = tasks.FirstOrDefault(x => x.TaskIdentifier == taskId);
            if (task != null)
                task.Cancel();
        }
    }
}