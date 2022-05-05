using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Threading.Tasks;
using Foundation;
using Microsoft.Extensions.Logging;


namespace Shiny.Net.Http
{
    public class HttpTransferManager : AbstractHttpTransferManager, IShinyStartupTask
    {
        readonly ShinyUrlSessionDelegate sessionDelegate;
        readonly NSUrlSessionConfiguration sessionConfig;
        readonly ILogger logger;
        readonly IPlatform platform;


        public HttpTransferManager(AppleLifecycle lifecycle,
                                  ILogger<IHttpTransferManager> logger,
                                  IPlatform platform,
                                  int maxConnectionsPerHost = 1)
        {
            this.platform = platform;
            this.logger = logger;

            this.sessionDelegate = new ShinyUrlSessionDelegate(this, logger, platform);
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

            var transfer = task.FromNative();
            task.Resume();

            return Task.FromResult(transfer);
        }


        protected override async Task<HttpTransfer> CreateUpload(HttpTransferRequest request)
        {
            if (request.HttpMethod != System.Net.Http.HttpMethod.Post && request.HttpMethod != System.Net.Http.HttpMethod.Put)
                throw new ArgumentException($"Invalid Upload HTTP Verb {request.HttpMethod} - only PUT or POST are valid");

            var boundary = Guid.NewGuid().ToString("N");
	            
            var native = request.ToNative();
            native["Content-Type"] = $"multipart/form-data; boundary={boundary}";

            var tempPath = platform.GetUploadTempFilePath(request);

            this.logger.LogInformation("Writing temp form data body to " + tempPath);

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
                    fs.WriteLine();
                    await uploadFile.CopyToAsync(fs);
                    fs.WriteLine();
                    fs.Write($"--{boundary}--");
                }
            }

            this.logger.LogInformation("Form body written");
            var tempFileUrl = NSUrl.CreateFileUrl(tempPath, null);

            var task = this.Session.CreateUploadTask(native, tempFileUrl);
            var taskId = TaskIdentifier.Create(request.LocalFile);
            task.TaskDescription = taskId.ToString();
            var transfer = task.FromNative();
            task.Resume();

            return transfer;
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