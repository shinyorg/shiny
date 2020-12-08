using System;
using System.IO;
using System.Reactive.Linq;
using System.Reactive.Disposables;
using System.Collections.Generic;
using System.Threading.Tasks;
using System.Net.Http;
using Shiny.Infrastructure;
using Android;
using Observable = System.Reactive.Linq.Observable;
using Native = Android.App.DownloadManager;


namespace Shiny.Net.Http
{
    public class HttpTransferManager : HttpClientHttpTransferManager
    {
        IObservable<HttpTransfer>? httpObs;


        public HttpTransferManager(ShinyCoreServices services) : base(services) {}


        public override async Task Cancel(string identifier)
        {
            await base.Cancel(identifier);
            if (Int64.TryParse(identifier, out var id))
                this.Services
                    .Android
                    .GetManager()
                    .Remove(id);
        }



        public override IObservable<HttpTransfer> WhenUpdated()
        {
            // TODO: cancel/error, should remove from db
            var query = new QueryFilter().ToNative();

            this.httpObs ??= Observable
                .Create<HttpTransfer>(ob =>
                {
                    var lastRun = DateTime.UtcNow;
                    var disposer = new CompositeDisposable();

                    HttpTransferBroadcastReceiver
                        .HttpEvents
                        .Subscribe(ob.OnNext)
                        .DisposedBy(disposer);

                    Observable
                        .Interval(TimeSpan.FromSeconds(2))
                        .Subscribe(_ =>
                        {
                            using (var cursor = this.Services.Android.GetManager().InvokeQuery(query))
                            {
                                while (cursor.MoveToNext())
                                {
                                    var lastModEpoch = cursor.GetLong(cursor.GetColumnIndex(Native.ColumnLastModifiedTimestamp));
                                    var epoch = DateTimeOffset.FromUnixTimeMilliseconds(lastModEpoch);
                                    if (epoch > lastRun)
                                    {
                                        var transfer = cursor.ToLib();
                                        ob.OnNext(transfer);
                                    }
                                }
                            }

                            lastRun = DateTime.UtcNow;
                        })
                        .DisposedBy(disposer);

                    return disposer;
                })
                .Publish()
                .RefCount();

            return this.httpObs.Merge(base.WhenUpdated());
        }


        protected override async Task<HttpTransfer> CreateDownload(HttpTransferRequest request)
        {
            if (request.HttpMethod != HttpMethod.Get)
                throw new ArgumentException("Only GETs are supported for downloads on Android");

            var access = await this.Services.Android.RequestAccess(Manifest.Permission.WriteExternalStorage);
            if (access != AccessState.Available)
                throw new ArgumentException("Invalid access to external storage - " + access);

            access = await this.Services.Android.RequestAccess(Manifest.Permission.ReadExternalStorage);
            if (access != AccessState.Available)
                throw new ArgumentException("Invalid access to external storage - " + access);

            var dlPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath;
            var path = Path.Combine(dlPath, request.LocalFile.Name);

            var native = new Native
                .Request(Android.Net.Uri.Parse(request.Uri))
                .SetDescription(request.LocalFile.FullName)
                .SetDestinationUri(ToNativeUri(path)) // WRITE_EXTERNAL_STORAGE
                .SetAllowedOverMetered(request.UseMeteredConnection);

            foreach (var header in request.Headers)
                native.AddRequestHeader(header.Key, header.Value);

            var id = this.Services.Android.GetManager().Enqueue(native);
            return new HttpTransfer(
                id.ToString(),
                request.Uri,
                request.LocalFile.FullName,
                false,
                request.UseMeteredConnection,
                null,
                0,
                0,
                HttpTransferState.Pending
            );
        }


        protected override Task<IEnumerable<HttpTransfer>> GetDownloads(QueryFilter filter)
            => Task.FromResult(this.GetAll(filter));


        IEnumerable<HttpTransfer> GetAll(QueryFilter filter)
        {
            var query = filter.ToNative();
            using (var cursor = this.Services.Android.GetManager().InvokeQuery(query))
                while (cursor.MoveToNext())
                    yield return cursor.ToLib();
        }


        static Android.Net.Uri ToNativeUri(string filePath)
        {
            var native = new Java.IO.File(filePath);
            return Android.Net.Uri.FromFile(native);
        }
    }
}
