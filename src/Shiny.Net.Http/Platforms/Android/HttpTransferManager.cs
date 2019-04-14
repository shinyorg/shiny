using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Collections.Generic;
using System.Threading.Tasks;
using Android.App;
using Android.Database;
using Observable = System.Reactive.Linq.Observable;
using Native = Android.App.DownloadManager;
using Shiny.Infrastructure;
using Shiny.Net.Http.Infrastructure;


namespace Shiny.Net.Http
{
    public class HttpTransferManager : AbstractHttpTransferManager
    {
        readonly IAndroidContext context;
        readonly IRepository repository;


        public HttpTransferManager(IAndroidContext context, IRepository repository)
        {
            this.context = context;
            this.repository = repository;
            //TODO: should I start intent service for receiver?
        }


        public override Task Cancel(string identifier)
        {
            var id = long.Parse(identifier);
            this.context
                .GetManager()
                .Remove(id);
            return Task.CompletedTask;
        }


        IObservable<HttpTransfer> httpObs;
        public override IObservable<HttpTransfer> WhenUpdated()
        {
            // TODO: cancel/error, should remove from db
            var query = ToNative(null);

            this.httpObs = this.httpObs ?? Observable
                .Create<HttpTransfer>(ob =>
                {
                    var lastRun = DateTime.UtcNow;
                    return Observable
                        .Interval(TimeSpan.FromSeconds(2))
                        .Subscribe(_ =>
                        {
                            using (var cursor = this.context.GetManager().InvokeQuery(query))
                            {
                                while (cursor.MoveToNext())
                                {
                                    var lastModEpoch = cursor.GetLong(cursor.GetColumnIndex(Native.ColumnLastModifiedTimestamp));
                                    var epoch = DateTimeOffset.FromUnixTimeMilliseconds(lastModEpoch);
                                    if (epoch > lastRun)
                                    {
                                        var transfer = ToLib(cursor);
                                        ob.OnNext(transfer);
                                    }
                                }
                            }

                            lastRun = DateTime.UtcNow;
                        });
                })
                .Publish()
                .RefCount();

            return this.httpObs;
        }


        protected override Task<HttpTransfer> CreateDownload(HttpTransferRequest request)
        {
            var dlPath = Android.OS.Environment.GetExternalStoragePublicDirectory(Android.OS.Environment.DirectoryDownloads).AbsolutePath;
            var path = Path.Combine(dlPath, request.LocalFile.Name);

            var native = new Native
                .Request(Android.Net.Uri.Parse(request.Uri))
                .SetDestinationUri(ToNativeUri(path)) // WRITE_EXTERNAL_STORAGE
                .SetAllowedOverMetered(request.UseMeteredConnection);

            foreach (var header in request.Headers)
                native.AddRequestHeader(header.Key, header.Value);

            var id = this.context.GetManager().Enqueue(native);
            return Task.FromResult(new HttpTransfer(
                id.ToString(),
                request.Uri,
                dlPath,
                false,
                request.UseMeteredConnection,
                null,
                null,
                0,
                0,
                HttpTransferState.Pending
            ));
        }


        protected override Task<IEnumerable<HttpTransfer>> GetUploads(QueryFilter filter)
        {
            return Task.FromResult(Enumerable.Empty<HttpTransfer>());
        }


        protected override Task<IEnumerable<HttpTransfer>> GetDownloads(QueryFilter filter)
            => Task.FromResult(this.GetAll(filter));


        IEnumerable<HttpTransfer> GetAll(QueryFilter filter)
        {
            var query = ToNative(filter);
            using (var cursor = this.context.GetManager().InvokeQuery(query))
                while (cursor.MoveToNext())
                    yield return ToLib(cursor);
        }


        static Android.Net.Uri ToNativeUri(string filePath)
        {
            var native = new Java.IO.File(filePath);
            return Android.Net.Uri.FromFile(native);
        }


        static Native.Query ToNative(QueryFilter filter)
        {
            var query = new Native.Query();
            if (filter != null)
            {
                if (filter.Ids?.Any() ?? false)
                {
                    var ids = filter.Ids.Select(long.Parse).ToArray();
                    query.SetFilterById(ids);
                }
                switch (filter.States)
                {
                    case HttpTransferStateFilter.Both:
                        query.SetFilterByStatus(DownloadStatus.Pending | DownloadStatus.Running);
                        break;

                    case HttpTransferStateFilter.Pending:
                        query.SetFilterByStatus(DownloadStatus.Pending);
                        break;

                    case HttpTransferStateFilter.InProgress:
                        query.SetFilterByStatus(DownloadStatus.Running);
                        break;
                }
            }
            return query;
        }


        static HttpTransfer ToLib(ICursor cursor)
        {
            Exception exception = null;
            var status = HttpTransferState.Unknown;
            var useMetered = true;
            var id = cursor.GetLong(cursor.GetColumnIndex(Native.ColumnId)).ToString();
            var fileSize = cursor.GetLong(cursor.GetColumnIndex(Native.ColumnTotalSizeBytes));
            var bytesTransferred = cursor.GetLong(cursor.GetColumnIndex(Native.ColumnBytesDownloadedSoFar));
            var uri = cursor.GetString(cursor.GetColumnIndex(Native.ColumnLocalUri));
            var localPath = cursor.GetString(cursor.GetColumnIndex(Native.ColumnLocalFilename));
            var nstatus = (DownloadStatus)cursor.GetInt(cursor.GetColumnIndex(Native.ColumnStatus));

            switch (nstatus)
            {
                case DownloadStatus.Failed:
                    exception = GetError(cursor);
                    status = HttpTransferState.Error;
                    break;

                case DownloadStatus.Paused:
                    status = GetPausedReason(cursor);
                    break;

                case DownloadStatus.Pending:
                    status = HttpTransferState.Pending;
                    break;

                case DownloadStatus.Running:
                    status = HttpTransferState.InProgress;
                    break;

                case DownloadStatus.Successful:
                    status = HttpTransferState.Completed;
                    break;
            }
            return new HttpTransfer(id, uri, localPath, false, useMetered, exception, "remoteFileName", fileSize, bytesTransferred, status);
        }


        static HttpTransferState GetPausedReason(ICursor cursor)
        {
            var reason = (DownloadPausedReason)cursor.GetInt(cursor.GetColumnIndex(Native.ColumnReason));
            switch (reason)
            {

                case DownloadPausedReason.QueuedForWifi:
                    return HttpTransferState.PausedByCostedNetwork;

                case DownloadPausedReason.WaitingForNetwork:
                    return HttpTransferState.PausedByNoNetwork;

                case DownloadPausedReason.WaitingToRetry:
                    return HttpTransferState.Retrying;

                case DownloadPausedReason.Unknown:
                default:
                    return HttpTransferState.Paused;
            }
        }


        static Exception GetError(ICursor cursor)
        {
            var error = (DownloadError)cursor.GetInt(cursor.GetColumnIndex(Native.ColumnReason));
            return new Exception(error.ToString());
        }
    }
}
