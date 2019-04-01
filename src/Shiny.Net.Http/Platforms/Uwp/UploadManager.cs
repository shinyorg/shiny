using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Windows.Networking.BackgroundTransfer;

namespace Shiny.Net.Http
{
    public class UploadManager : IUploadManager
    {
        public Task Cancel(IHttpTransfer transfer)
        {
            throw new NotImplementedException();
        }


        public async Task CancelAll()
        {
            var uploads = await BackgroundUploader.GetCurrentUploadsAsync();
            foreach (var upload in uploads)
                upload.AttachAsync().Cancel();
        }


        public Task<IHttpTransfer> Create(HttpTransferRequest request)
        {
  //public override IHttpTransfer Upload(HttpTransferConfiguration config)
  //      {

  //          if (String.IsNullOrWhiteSpace(config.LocalFilePath))
  //              throw new ArgumentException("You must set the local file path when uploading");

  //          if (!File.Exists(config.LocalFilePath))
  //              throw new ArgumentException($"File '{config.LocalFilePath}' does not exist");

  //          var task = new BackgroundUploader
  //          {
  //              Method = config.HttpMethod,
  //              CostPolicy = config.UseMeteredConnection
  //                  ? BackgroundTransferCostPolicy.Default
  //                  : BackgroundTransferCostPolicy.UnrestrictedOnly
  //          };

  //          foreach (var header in config.Headers)
  //              task.SetRequestHeader(header.Key, header.Value);

  //          // seriously - this should not be async!
  //          var file = StorageFile.GetFileFromPathAsync(config.LocalFilePath).AsTask().Result;
  //          var operation = task.CreateUpload(new Uri(config.Uri), file);
  //          var httpTask = new UploadHttpTransfer(config, operation, false);
  //          this.Add(httpTask);

  //          return httpTask;
  //      }
            //BackgroundUploader
            //               .GetCurrentUploadsAsync()
            //               .AsTask()
            //               .ContinueWith(result =>
            //               {
            //                   foreach (var task in result.Result)
            //                   {
            //                       var config = new HttpTransferConfiguration(task.RequestedUri.ToString(), task.SourceFile.Path)
            //                       {
            //                           HttpMethod = task.Method,
            //                           UseMeteredConnection = task.CostPolicy != BackgroundTransferCostPolicy.UnrestrictedOnly
            //                       };
            //                       this.Add(new UploadHttpTransfer(config, task, true));
            //                   }
            //               });
            throw new NotImplementedException();
        }

        public Task<IEnumerable<IHttpTransfer>> GetTransfers()
        {
            throw new NotImplementedException();
        }
    }
}
