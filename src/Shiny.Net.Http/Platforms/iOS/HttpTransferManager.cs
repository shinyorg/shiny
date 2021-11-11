using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Extensions.Logging;


namespace Shiny.Net.Http
{
    public class HttpTransferManager : AbstractHttpTransferManager, IShinyStartupTask
    {
        readonly ShinyUrlSessionDelegate sessionDelegate;
        readonly NSUrlSessionConfiguration sessionConfig;
        readonly IPlatform platform;


        public HttpTransferManager(AppleLifecycle lifecycle,
                                  ILogger<IHttpTransferManager> logger,
                                  IPlatform platform,
                                  int maxConnectionsPerHost = 1)
        {
            this.platform = platform;

            this.sessionDelegate = new ShinyUrlSessionDelegate(this, logger);
            this.sessionConfig = NSUrlSessionConfiguration.CreateBackgroundSessionConfiguration(SessionName);
            this.sessionConfig.HttpMaximumConnectionsPerHost = maxConnectionsPerHost;
            this.sessionConfig.RequestCachePolicy = NSUrlRequestCachePolicy.ReloadIgnoringLocalAndRemoteCacheData;

            var s = this.Session; // force load

            lifecycle.RegisterHandleEventsForBackgroundUrl((sessionIdentifier, completionHandler) =>
            {
                if (!SessionName.Equals(sessionIdentifier))
                    return false;

                ShinyUrlSessionDelegate.CompletionHandler = completionHandler;
                return true;
            });
        }


        public void Start()
        {
            // this is just to fire off the constructor
        }


        static string SessionName => $"{NSBundle.MainBundle.BundleIdentifier}.BackgroundTransferSession";
        internal void CompleteSession() => this.session = null;


        protected override Task<HttpTransfer> CreateDownload(HttpTransferRequest request)
        {
            var task = this.Session.CreateDownloadTask(request.ToNative());
            var taskId = TaskIdentifier.Create(request.LocalFile);
            task.TaskDescription = taskId.ToString();
            //task.Response.SuggestedFilename
            //task.Response.ExpectedContentLength

            var transfer = task.FromNative();
            task.Resume();

            return Task.FromResult(transfer);
        }


        protected override Task<HttpTransfer> CreateUpload(HttpTransferRequest request)
        {
            if (request.HttpMethod != System.Net.Http.HttpMethod.Post && request.HttpMethod != System.Net.Http.HttpMethod.Put)
                throw new ArgumentException($"Invalid Upload HTTP Verb {request.HttpMethod} - only PUT or POST are valid");

            var native = request.ToNative();

            var boundary = Guid.NewGuid().ToString();
            var tempPath = Path.Combine(this.platform.Cache.FullName, request.LocalFile.Name + ".tmp");
            using (var fs = new FileStream(tempPath, FileMode.Create))
            {
                if (!request.PostData.IsEmpty())
                {
                    fs.Write("--" + boundary);
                    fs.Write($"Content-Type: text/plain; charset=utf-8");
                    fs.Write("Content-Disposition: form-data;");
                    fs.WriteLine();
                    fs.Write(request.PostData!);
                    fs.WriteLine();
                }
            }
            using (var fs = new FileStream(tempPath, FileMode.Append))
            {
                using (var uploadFile = request.LocalFile.OpenRead())
                {
                    fs.Write("--" + boundary);
                    fs.Write($"Content-Type: application/octet-stream");
                    fs.Write($"Content-Disposition: form-data; name=\"blob\"; filename=\"{request.LocalFile.Name}\"");

                    uploadFile.CopyToAsync(fs);
                    fs.Write($"--{boundary}--");
                }
            }
            var tempFileUrl = NSUrl.CreateFileUrl(tempPath, null);
            var task = this.Session.CreateUploadTask(native, tempFileUrl);

            var taskId = TaskIdentifier.Create(request.LocalFile);
            task.TaskDescription = taskId.ToString();
            var transfer = task.FromNative();
            task.Resume();

            return Task.FromResult(transfer);
        }


        public override IObservable<HttpTransfer> WhenUpdated()
            => this.sessionDelegate.WhenEventOccurs();


        public override Task<IEnumerable<HttpTransfer>> GetTransfers(QueryFilter? filter = null)
            => this.Session.QueryTransfers(filter);


        public override async Task Cancel(QueryFilter? filter = null)
        {
            var tasks = await this.Session.QueryTasks(filter);
            foreach (var task in tasks)
                task.Cancel();
        }


        public override async Task Cancel(string id)
        {
            var tasks = await this.Session.GetAllTasksAsync();
            var task = tasks.FirstOrDefault(x => x.TaskDescription.StartsWith(id + "|"));

            if (task != null)
                task.Cancel();
        }


        protected string ToTaskDescription(FileInfo file)
            => $"{Guid.NewGuid()}|{file.FullName}";


        //this.sessionConfig.Discretionary = true;
        //this.sessionConfig.HttpShouldUsePipelining = true;
        //this.sessionConfig.RequestCachePolicy = NSUrlRequestCachePolicy.ReloadIgnoringCacheData;
        //this.sessionConfig.ShouldUseExtendedBackgroundIdleMode = true;
        NSUrlSession? session;
        internal NSUrlSession Session => this.session ??= NSUrlSession.FromConfiguration(
            this.sessionConfig,
            (INSUrlSessionDownloadDelegate) this.sessionDelegate,
            new NSOperationQueue()
        );
    }
}