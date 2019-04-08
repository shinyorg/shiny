using System;
using Android.App;
using Android.Database;
using Shiny.Net.Http.Infrastructure;
using Native = Android.App.DownloadManager;


namespace Shiny.Net.Http
{
    class HttpTransfer : AbstractHttpTransfer
    {
        public HttpTransfer(HttpTransferRequest request, string id) : base(request, id)
        {
        }


        public string TempFile { get; set; }


        internal void Refresh(ICursor cursor)
        {
            this.FileSize = cursor.GetLong(cursor.GetColumnIndex(Native.ColumnTotalSizeBytes));
            this.BytesTransferred = cursor.GetLong(cursor.GetColumnIndex(Native.ColumnBytesDownloadedSoFar));
            //Native.ColumnLocalUri
            //cursor.GetString(cursor.GetColumnIndex(Native.ColumnLocalFilename));

            var nstatus = (DownloadStatus)cursor.GetInt(cursor.GetColumnIndex(Native.ColumnStatus));
            switch (nstatus)
            {
                case DownloadStatus.Failed:
                    this.SetError(cursor);
                    break;

                case DownloadStatus.Paused:
                    this.SetPaused(cursor);
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


        void SetPaused(ICursor cursor)
        {
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
        }


        void SetError(ICursor cursor)
        {
            this.Status = HttpTransferState.Error;
            var msg = "There was an error with the request";
            var error = (DownloadError)cursor.GetInt(cursor.GetColumnIndex(Native.ColumnReason));
            switch (error)
            {
                case DownloadError.CannotResume:
                    break;

                case DownloadError.DeviceNotFound:
                    break;

                case DownloadError.FileAlreadyExists:
                    break;

                case DownloadError.FileError:
                    break;

                case DownloadError.HttpDataError:
                    break;

                case DownloadError.InsufficientSpace:
                    break;

                case DownloadError.TooManyRedirects:
                    break;

                case DownloadError.UnhandledHttpCode:
                    break;

                case DownloadError.Unknown:
                default:
                    break;
            }
            this.Exception = new Exception(msg);
        }
    }
}
