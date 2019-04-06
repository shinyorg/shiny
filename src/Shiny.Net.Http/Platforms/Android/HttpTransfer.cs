using System;
using Android.App;
using Android.Database;
using Native = Android.App.DownloadManager;


namespace Shiny.Net.Http
{
    class HttpTransfer : AbstractHttpTransfer
    {
        public HttpTransfer(HttpTransferRequest request, string id) : base(request, id)
        {
        }


        internal void Refresh(ICursor cursor)
        {
            //Native.ColumnLocalUri
            //cursor.GetString(cursor.GetColumnIndex(Native.ColumnLocalFilename));
            var nstatus = (DownloadStatus)cursor.GetInt(cursor.GetColumnIndex(Native.ColumnStatus));
            switch (nstatus)
            {
                case DownloadStatus.Failed:
                    // TODO: get error reason
                    this.Exception = new Exception("There was an error with the request");
                    this.Status = HttpTransferState.Error;
                    break;

                case DownloadStatus.Paused:
                    var reason = (DownloadPausedReason)cursor.GetInt(cursor.GetColumnIndex(Native.ColumnReason));
                    switch (reason)
                    {
                        case DownloadPausedReason.Unknown:
                            this.Status = HttpTransferState.Paused;
                            break;

                        case DownloadPausedReason.QueuedForWifi:
                            this.Status = HttpTransferState.PausedByCostedNetwork;
                            break;

                        case DownloadPausedReason.WaitingForNetwork:
                            this.Status = HttpTransferState.PausedByNoNetwork;
                            break;

                        case DownloadPausedReason.WaitingToRetry:
                            this.Status = HttpTransferState.Retrying;
                            break;
                    }
                    break;

                case DownloadStatus.Pending:
                    this.Status = HttpTransferState.Pending;
                    break;

                case DownloadStatus.Running:
                    this.FileSize = cursor.GetLong(cursor.GetColumnIndex(Native.ColumnTotalSizeBytes));
                    this.BytesTransferred = cursor.GetLong(cursor.GetColumnIndex(Native.ColumnBytesDownloadedSoFar));
                    break;

                case DownloadStatus.Successful:
                    this.Status = HttpTransferState.Completed;
                    break;
            }
        }
    }
}
