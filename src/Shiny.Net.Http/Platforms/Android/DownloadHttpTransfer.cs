using System;
using Android.App;
using Android.Database;
using Native = Android.App.DownloadManager;


namespace Shiny.Net.Http
{
    public class DownloadHttpTransfer : AbstractHttpTransfer
    {
        public DownloadHttpTransfer(HttpTransferRequest request, long id) : base(request, false)
        {
            this.Identifier = id.ToString();
            this.Status = HttpTransferState.Unknown;
        }


        internal void Refresh(ICursor cursor)
        {
            this.FileSize = cursor.GetLong(cursor.GetColumnIndex(Native.ColumnTotalSizeBytes));
            this.BytesTransferred = cursor.GetLong(cursor.GetColumnIndex(Native.ColumnBytesDownloadedSoFar));

            //var reason = cursor.GetString(cursor.GetColumnIndex(Native.ColumnReason));
            //var lastMod = cursor.GetString(cursor.GetColumnIndex(Native.ColumnLastModifiedTimestamp));
            var status = (DownloadStatus)cursor.GetInt(cursor.GetColumnIndex(Native.ColumnStatus));

            switch (status)
            {
                case DownloadStatus.Failed:
                    this.Status = HttpTransferState.Error;
                    // TODO: get error
                    break;

                case DownloadStatus.Paused:
                    //Native.ColumnReason
                    //Native.PausedQueuedForWifi
                    //Native.PausedUnknown
                    // TODO: get paused reason
                    this.Status = HttpTransferState.Paused;
                    break;

                // TODO: could use a pending state
                case DownloadStatus.Pending:
                case DownloadStatus.Running:
                    this.Status = HttpTransferState.Running;
                    break;

                case DownloadStatus.Successful:
                    this.Status = HttpTransferState.Completed;
                    break;
            }
            this.RunCalculations();
        }
    }
}
