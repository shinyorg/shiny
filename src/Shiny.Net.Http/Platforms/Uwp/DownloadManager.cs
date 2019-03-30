using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.IO;
using Windows.Networking.BackgroundTransfer;
using Windows.Storage;


namespace Shiny.Net.Http
{
    public class DownloadManager : IDownloadManager
    {
        readonly IFileSystem fileSystem;

        public DownloadManager(IFileSystem fileSystem)
        {
            this.fileSystem = fileSystem;
        }


        public async Task CancelAll()
        {
            var tasks = await BackgroundDownloader.GetCurrentDownloadsAsync().AsTask();
            foreach (var task in tasks)
                task.AttachAsync().Cancel();
        }


        public async Task<IHttpTransfer> Create(HttpTransferRequest request)
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

            //var filePath = config.LocalFilePath ?? Path.Combine(ApplicationData.Current.LocalFolder.Path, Path.GetRandomFileName());
            var winFile = await StorageFile.GetFileFromPathAsync(request.LocalFilePath.FullName).AsTask();
            var op = task.CreateDownload(new Uri(request.Uri), winFile);

            //var operation = task.CreateDownload(new Uri(config.Uri), file);
            //var httpTask = new DownloadHttpTask(config, operation, false);
            //this.Add(httpTask);
            return null;
        }

        public async Task<IEnumerable<IHttpTransfer>> GetTransfers()
        {
            var downloads = await BackgroundDownloader.GetCurrentDownloadsAsync().AsTask();

            return null;
        }
    }
}