using System;
using System.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Reactive.Linq;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;
using Microsoft.Extensions.Logging;


namespace Shiny.Net.Http
{
    public class HttpTransferManager : AbstractHttpTransferManager, IShinyStartupTask
    {
        readonly ILogger logger;
        public HttpTransferManager(ILogger<IHttpTransferManager> logger) => this.logger = logger;


        protected override async Task<IEnumerable<HttpTransfer>> GetDownloads(QueryFilter filter)
        {
            var items = await BackgroundDownloader
                .GetCurrentDownloadsAsync()
                .AsTask();

            //x.GetResponseInformation().IsResumable
            return items
                .Select(x => x.FromNative())
                .ToList();
        }


        protected override async Task<IEnumerable<HttpTransfer>> GetUploads(QueryFilter filter)
        {
            var items = await BackgroundUploader
                .GetCurrentUploadsAsync()
                .AsTask();

            return items
                .Select(x => x.FromNative())
                .ToList();
        }


        protected override async Task<HttpTransfer> CreateUpload(HttpTransferRequest request)
        {
            var task = new BackgroundUploader
            {
                Method = request.HttpMethod.Method,
                CostPolicy = request.UseMeteredConnection
                    ? BackgroundTransferCostPolicy.Default
                    : BackgroundTransferCostPolicy.UnrestrictedOnly
            };
            foreach (var header in request.Headers)
                task.SetRequestHeader(header.Key, header.Value);

            var winFile = await StorageFile.GetFileFromPathAsync(request.LocalFile.FullName).AsTask();
            var operation = task.CreateUpload(new Uri(request.Uri), winFile);
            operation.StartAsync();

            return operation.FromNative();
        }


        protected override async Task<HttpTransfer> CreateDownload(HttpTransferRequest request)
        {
            var task = new BackgroundDownloader
            {
                Method = request.HttpMethod.Method,
                CostPolicy = request.UseMeteredConnection
                    ? BackgroundTransferCostPolicy.Default
                    : BackgroundTransferCostPolicy.UnrestrictedOnly
            };
            foreach (var header in request.Headers)
                task.SetRequestHeader(header.Key, header.Value);

            if (!request.LocalFile.Exists)
                request.LocalFile.Create();

            var winFile = await StorageFile.GetFileFromPathAsync(request.LocalFile.FullName).AsTask();
            var operation = task.CreateDownload(new Uri(request.Uri), winFile);
            operation.StartAsync();

            return operation.FromNative();
        }


        public override async Task Cancel(string id)
        {
            var guid = Guid.Parse(id);
            var tasks = await BackgroundDownloader
                .GetCurrentDownloadsAsync()
                .AsTask();

            var task = tasks.FirstOrDefault(x => x.Guid == guid);
            if (task != null)
                task.AttachAsync().Cancel();
        }


        public override IObservable<HttpTransfer> WhenUpdated() => Observable.Create<HttpTransfer>(async (ob, ct) =>
        {
            var downloads = await BackgroundDownloader
                .GetCurrentDownloadsAsync()
                .AsTask();

            foreach (var download in downloads)
            {
                download
                    .AttachAsync()
                    .AsTask(
                        ct,
                        new Progress<DownloadOperation>(_ =>
                            ob.OnNext(download.FromNative())
                        )
                    );
            }

            var uploads = await BackgroundUploader
                .GetCurrentUploadsAsync()
                .AsTask();

            foreach (var upload in uploads)
            {
                upload
                    .AttachAsync()
                    .AsTask(
                        ct,
                        new Progress<UploadOperation>(_ =>
                            ob.OnNext(upload.FromNative())
                        )
                    );
            }
        });

        public async void Start()
        {
            try
            {
                var downloads = await BackgroundDownloader
                    .GetCurrentDownloadsAsync()
                    .AsTask();

                foreach (var dl in downloads)
                    dl.Resume();

                var uploads = await BackgroundUploader
                    .GetCurrentUploadsAsync()
                    .AsTask();

                foreach (var ul in uploads)
                    ul.StartAsync();
            }
            catch (Exception ex)
            {
                this.logger.LogError(ex, "Error restarting HTTP transfers");
            }
        }
    }
}
// TODO: call to delegate
/*
 var completionGroup = new BackgroundTransferCompletionGroup();
BackgroundTaskBuilder builder = new BackgroundTaskBuilder();

builder.Name = "MyDownloadProcessingTask";
builder.SetTrigger(completionGroup.Trigger);
builder.TaskEntryPoint = "Tasks.BackgroundDownloadProcessingTask";

BackgroundTaskRegistration downloadProcessingTask = builder.Register();
Next you associate background transfers with the completion group. Once all transfers are created, enable the completion group.
C#

Copy
BackgroundDownloader downloader = new BackgroundDownloader(completionGroup);
DownloadOperation download = downloader.CreateDownload(uri, file);
Task<DownloadOperation> startTask = download.StartAsync().AsTask();

// App still sees the normal completion path
startTask.ContinueWith(ForegroundCompletionHandler);

// Do not enable the CompletionGroup until after all downloads are created.
downloader.CompletinGroup.Enable();
The code in the background task extracts the list of operations from the trigger details, and your code can then inspect the details for each operation and perform appropriate post-processing for each operation.
C#

Copy
public class BackgroundDownloadProcessingTask : IBackgroundTask
{
    public async void Run(IBackgroundTaskInstance taskInstance)
    {
    var details = (BackgroundTransferCompletionGroupTriggerDetails)taskInstance.TriggerDetails;
    IReadOnlyList<DownloadOperation> downloads = details.Downloads;

    // Do post-processing on each finished operation in the list of downloads
    }
}
*/
