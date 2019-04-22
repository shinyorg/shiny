using System;
using System.Linq;
using Android.App;
using Android.Content;
using Android.Database;
using Native = Android.App.DownloadManager;


namespace Shiny.Net.Http
{
    static class PlatformExtensions
    {
        static Native downloadManager;
        public static Native GetManager(this AndroidContext context)
            => context.AppContext.GetManager();


        public static Native GetManager(this Context context)
        {
            if (downloadManager == null || downloadManager.Handle == IntPtr.Zero)
                downloadManager = (Native)context.GetSystemService(Context.DownloadService);

            return downloadManager;
        }


        public static Native.Query ToNative(this QueryFilter filter)
        {
            var query = new Native.Query();
            if (filter != null)
            {
                if (filter.Ids?.Any() ?? false)
                {
                    var ids = filter.Ids.Select(long.Parse).ToArray();
                    query.SetFilterById(ids);
                }
                //switch (filter.States)
                //{
                //    case HttpTransferStateFilter.Both:
                //        query.SetFilterByStatus(DownloadStatus.Pending | DownloadStatus.Running);
                //        break;

                //    case HttpTransferStateFilter.Pending:
                //        query.SetFilterByStatus(DownloadStatus.Pending);
                //        break;

                //    case HttpTransferStateFilter.InProgress:
                //        query.SetFilterByStatus(DownloadStatus.Running);
                //        break;
                //}
            }
            return query;
        }


        public static HttpTransfer ToLib(this ICursor cursor)
        {
            Exception exception = null;
            var status = HttpTransferState.Unknown;
            var useMetered = true;
            var id = cursor.GetLong(cursor.GetColumnIndex(Native.ColumnId)).ToString();
            var fileSize = cursor.GetLong(cursor.GetColumnIndex(Native.ColumnTotalSizeBytes));
            var bytesTransferred = cursor.GetLong(cursor.GetColumnIndex(Native.ColumnBytesDownloadedSoFar));
            var uri = cursor.GetString(cursor.GetColumnIndex(Native.ColumnLocalUri));
            //var localPath = cursor.GetString(cursor.GetColumnIndex(Native.ColumnLocalFilename));
            var localPath = cursor.GetString(cursor.GetColumnIndex(Native.ColumnDescription)); // temp piggybacking
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
            // TODO: I want to move out of local path, to requested path
            return new HttpTransfer(id, uri, localPath, false, useMetered, exception, fileSize, bytesTransferred, status);
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
