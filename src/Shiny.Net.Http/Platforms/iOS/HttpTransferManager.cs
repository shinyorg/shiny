using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Foundation;


namespace Shiny.Net.Http
{
    public class HttpTransferManager : IHttpTransferManager
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


        public Task Cancel(IHttpTransfer transfer)
        {
            var t = (HttpTransfer)transfer;
            //if (t.Status == HttpTransferState.Running)

            if (t.UploadTask == null)
                t.DownloadTask.Cancel();
            else
                t.UploadTask.Cancel();

            // TODO: remove from queue
            t.Status = HttpTransferState.Cancelled;
            return Task.CompletedTask;
        }


        public async Task CancelAll()
        {

            throw new NotImplementedException();
        }


        public Task<IHttpTransfer> Enqueue(HttpTransferRequest request)
        {
            //var task = this.session.CreateUploadTask(request.ToNative());

            var task = this.session.CreateDownloadTask(request.ToNative());
            new HttpTransfer(request, task);

            return null;
        }

        public Task<IEnumerable<IHttpTransfer>> GetTransfers()
        {
            //this.session.GetTasks2((_, uploads, downloads) =>
            //{
            //    foreach (NSUrlSessionUploadTask upload in uploads)
            //    {
            //        // TODO: need localFilePath for what WAS uploading
            //        // TODO: need to set resumed status
            //        //this.Add(new HttpTask(this.ToTaskConfiguration(upload), upload));
            //        upload.Resume();
            //    }

            //    foreach (var download in downloads)
            //    {
            //        //this.Add(new HttpTask(this.ToTaskConfiguration(download), download));
            //        download.Resume();
            //    }
            //});
            return null;
        }


        //protected virtual HttpTransferConfiguration ToTaskConfiguration(NSUrlSessionTask native)
        //    => new HttpTransferConfiguration(native.OriginalRequest.ToString())
        //    {
        //        UseMeteredConnection = native.OriginalRequest.AllowsCellularAccess,
        //        HttpMethod = native.OriginalRequest.HttpMethod,
        //        PostData = native.OriginalRequest.Body.ToString(),
        //        Headers = native.OriginalRequest.Headers.ToDictionary(
        //            x => x.Key.ToString(),
        //            x => x.Value.ToString()
        //        )
        //    };
    }
}