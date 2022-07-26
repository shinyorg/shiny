//using System;
//using System.Linq;
//using Android.App;
//using Android.Content;
//using Android.Database;
//using Native = Android.App.DownloadManager;

//namespace Shiny.Net.Http;


//static class PlatformExtensions
//{
//    public static string GetString(this ICursor cursor, string column)
//        => cursor.GetString(cursor.GetColumnIndex(column));

//    public static int GetInt(this ICursor cursor, string column)
//        => cursor.GetInt(cursor.GetColumnIndex(column));

//    public static long GetLong(this ICursor cursor, string column)
//        => cursor.GetLong(cursor.GetColumnIndex(column));


//    public static Native GetManager(this AndroidPlatform platform)
//        => platform.GetSystemService<Native>(Context.DownloadService);


//    public static Native.Query ToNative(this QueryFilter filter)
//    {
//        var query = new Native.Query();
//        if (filter != null)
//        {
//            if (filter.Ids?.Any() ?? false)
//            {
//                var ids = filter.Ids.Select(long.Parse).ToArray();
//                query.SetFilterById(ids);
//            }
//            //switch (filter.States)
//            //{
//            //    case HttpTransferStateFilter.Both:
//            //        query.SetFilterByStatus(DownloadStatus.Pending | DownloadStatus.Running);
//            //        break;

//            //    case HttpTransferStateFilter.Pending:
//            //        query.SetFilterByStatus(DownloadStatus.Pending);
//            //        break;

//            //    case HttpTransferStateFilter.InProgress:
//            //        query.SetFilterByStatus(DownloadStatus.Running);
//            //        break;
//            //}
//        }
//        return query;
//    }


//    public static HttpTransfer ToLib(this ICursor cursor)
//    {
//        Exception? exception = null;
//        var status = HttpTransferState.Unknown;
//        var useMetered = true;
//        var id = cursor.GetLong(Native.ColumnId).ToString();
//        var fileSize = cursor.GetLong(Native.ColumnTotalSizeBytes);
//        var bytesTransferred = cursor.GetLong(Native.ColumnBytesDownloadedSoFar);
//        var uri = cursor.GetString(Native.ColumnUri);
//        var localPath = cursor.GetString(Native.ColumnDescription); // temp piggybacking
//        var nstatus = (DownloadStatus)cursor.GetInt(Native.ColumnStatus);

//        switch (nstatus)
//        {
//            case DownloadStatus.Failed:
//                exception = GetError(cursor);
//                status = HttpTransferState.Error;
//                break;

//            case DownloadStatus.Paused:
//                status = GetPausedReason(cursor);
//                break;

//            case DownloadStatus.Pending:
//                status = HttpTransferState.Pending;
//                break;

//            case DownloadStatus.Running:
//                status = HttpTransferState.InProgress;
//                break;

//            case DownloadStatus.Successful:
//                status = HttpTransferState.Completed;
//                break;
//        }
//        return new HttpTransfer(id, uri, localPath, false, useMetered, exception, fileSize, bytesTransferred, status);
//    }


//    static HttpTransferState GetPausedReason(ICursor cursor)
//    {
//        var reason = (DownloadPausedReason)cursor.GetInt(Native.ColumnReason);
//        return reason switch
//        {
//            DownloadPausedReason.QueuedForWifi => HttpTransferState.PausedByCostedNetwork,
//            DownloadPausedReason.WaitingForNetwork => HttpTransferState.PausedByNoNetwork,
//            DownloadPausedReason.WaitingToRetry => HttpTransferState.Retrying,
//            DownloadPausedReason.Unknown => HttpTransferState.Unknown,
//            _ => HttpTransferState.Paused
//        };
//    }


//    static Exception GetError(ICursor cursor)
//    {
//        var errorId = cursor.GetInt(Native.ColumnReason);
//        var error = (DownloadError)errorId;
//        return new Exception(error.ToString());
//    }
//}
