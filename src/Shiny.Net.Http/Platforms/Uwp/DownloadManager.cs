using System;
using System.Collections.Generic;
using System.Threading.Tasks;


namespace Shiny.Net.Http
{
    public class DownloadManager : IDownloadManager
    {
        public Task CancelAll()
        {
            throw new NotImplementedException();
        }

        public Task<IHttpTransfer> Create(HttpTransferRequest request)
        {
            //BackgroundDownloader
            //    .GetCurrentDownloadsAsync()
            //    .AsTask()
            //    .ContinueWith(result =>
            //    {
            //        foreach (var task in result.Result)
            //        {
            //            var config = new HttpTransferConfiguration(task.RequestedUri.ToString())
            //            {
            //                HttpMethod = task.Method,
            //                UseMeteredConnection = task.CostPolicy != BackgroundTransferCostPolicy.UnrestrictedOnly
            //            };
            //            this.Add(new DownloadHttpTask(config, task, true));
            //        }
            //    });
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IHttpTransfer>> GetTransfers()
        {
            throw new NotImplementedException();
        }
    }
}
/*
  public override IHttpTransfer Upload(HttpTransferConfiguration config)
        {

            if (String.IsNullOrWhiteSpace(config.LocalFilePath))
                throw new ArgumentException("You must set the local file path when uploading");

            if (!File.Exists(config.LocalFilePath))
                throw new ArgumentException($"File '{config.LocalFilePath}' does not exist");

            var task = new BackgroundUploader
            {
                Method = config.HttpMethod,
                CostPolicy = config.UseMeteredConnection
                    ? BackgroundTransferCostPolicy.Default
                    : BackgroundTransferCostPolicy.UnrestrictedOnly
            };

            foreach (var header in config.Headers)
                task.SetRequestHeader(header.Key, header.Value);

            // seriously - this should not be async!
            var file = StorageFile.GetFileFromPathAsync(config.LocalFilePath).AsTask().Result;
            var operation = task.CreateUpload(new Uri(config.Uri), file);
            var httpTask = new UploadHttpTransfer(config, operation, false);
            this.Add(httpTask);

            return httpTask;
        }


        public override IHttpTransfer Download(HttpTransferConfiguration config)
        {
            var task = new BackgroundDownloader
            {
                Method = config.HttpMethod,
                CostPolicy = config.UseMeteredConnection
                    ? BackgroundTransferCostPolicy.Default
                    : BackgroundTransferCostPolicy.UnrestrictedOnly
            };
            foreach (var header in config.Headers)
                task.SetRequestHeader(header.Key, header.Value);

            var filePath = config.LocalFilePath ?? Path.Combine(ApplicationData.Current.LocalFolder.Path, Path.GetRandomFileName());
            var fileName = Path.GetFileName(filePath);
            var directory = Path.GetDirectoryName(filePath);

            // why are these async??
            var folder = StorageFolder.GetFolderFromPathAsync(directory).AsTask().Result;
            var file = folder.CreateFileAsync(fileName).AsTask().Result;

            var operation = task.CreateDownload(new Uri(config.Uri), file);
            var httpTask = new DownloadHttpTask(config, operation, false);
            this.Add(httpTask);

            return httpTask;
        }
     */
