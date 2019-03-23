using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Net.Http.Internals;
using Foundation;


namespace Shiny.Net.Http
{
    public class DownloadManager : IDownloadManager
    {
        readonly IRepository repository;
        readonly CoreSessionDownloadDelegate sessionDelegate;
        readonly NSUrlSessionConfiguration sessionConfig;
        readonly NSUrlSession session;


        public DownloadManager(IRepository repository)
        {
            this.repository = repository;
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
                NSUrlRequest.FromUrl(NSUrl.FromFilename(request.Uri)),
                NSUrl.FromFilename(request.LocalFilePath.FullName)
            );
            return null;
        }

        public Task<IEnumerable<IHttpTransfer>> GetTransfers()
        {
            throw new NotImplementedException();
        }


        //public override IHttpTransfer Upload(HttpTransferConfiguration config)
        //{
        //    var request = this.CreateRequest(config);
        //    var native = this.session.CreateUploadTask(request, NSUrl.FromFilename(config.LocalFilePath));
        //    var task = new HttpTask(config, native);
        //    this.Add(task);
        //    native.Resume();

        //    return task;
        //}


        //public override IHttpTransfer Download(HttpTransferConfiguration config)
        //{
        //    var request = this.CreateRequest(config);
        //    var native = this.session.CreateDownloadTask(request);
        //    var task = new HttpTask(config, native);
        //    this.Add(task);
        //    native.Resume();

        //    return task;
        //}


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


        //protected override void Add(IHttpTransfer task)
        //{
        //    if (!(task is IIosHttpTransfer ios))
        //        throw new ArgumentException("You must inherit from IIosHttpTask");

        //    this.sessionDelegate.AddTask(ios);
        //    base.Add(task);
        //}


        //protected virtual NSUrlRequest CreateRequest(HttpTransferConfiguration config)
        //{
        //    var url = NSUrl.FromString(config.Uri);
        //    var request = new NSMutableUrlRequest(url)
        //    {
        //        HttpMethod = config.HttpMethod,
        //        AllowsCellularAccess = config.UseMeteredConnection
        //    };

        //    if (!String.IsNullOrWhiteSpace(config.PostData))
        //        request.Body = NSData.FromString(config.PostData);

        //    foreach (var header in config.Headers)
        //    {
        //        request.Headers.SetValueForKey(
        //            new NSString(header.Value),
        //            new NSString(header.Key)
        //        );
        //    }
        //    return request;
        //}
    }
}