using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Net.Http.Infrastructure;
using Foundation;


namespace Shiny.Net.Http
{
    public class UploadManager : IUploadManager
    {
        readonly IRepository repository;
        readonly CoreSessionDownloadDelegate sessionDelegate;
        readonly NSUrlSessionConfiguration sessionConfig;
        readonly NSUrlSession session;


        public UploadManager()
        {
            this.sessionDelegate = new CoreSessionDownloadDelegate();
            this.sessionConfig = NSUrlSessionConfiguration.CreateBackgroundSessionConfiguration(NSBundle.MainBundle.BundleIdentifier + ".BackgroundTransferSession");
            this.session = NSUrlSession.FromConfiguration(
                this.sessionConfig,
                this.sessionDelegate,
                new NSOperationQueue()
            );

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
        }

        public async Task CancelAll()
        {
            await this.repository.GetAll<HttpTransferStore>();

            throw new NotImplementedException();
        }


        public Task<IHttpTransfer> Create(HttpTransferRequest request)
        {
            var task = this.session.CreateUploadTask(
                request.ToNative(),
                NSUrl.FromFilename(request.LocalFilePath.FullName)
            );
            return null;
        }

        public Task<IEnumerable<IHttpTransfer>> GetTransfers()
        {
            throw new NotImplementedException();
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