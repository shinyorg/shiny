using System;
using Android.App;
using Android.Database;
using Native = Android.App.DownloadManager;


namespace Shiny.Net.Http
{
    class HttpTransfer : AbstractHttpTransfer
    {
        public HttpTransfer(HttpTransferRequest request, string id) : base(request)
        {
            this.Identifier = id;
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
                    // TODO: get reason
                    this.Status = HttpTransferState.Paused;
                    break;

                case DownloadStatus.Pending:
                    this.Status = HttpTransferState.Pending;
                    break;

                case DownloadStatus.Running:
                    //this.RemoteFileName
                    this.FileSize = cursor.GetLong(cursor.GetColumnIndex(Native.ColumnTotalSizeBytes));
                    this.BytesTransferred = cursor.GetLong(cursor.GetColumnIndex(Native.ColumnBytesDownloadedSoFar));
                    this.RunCalculations();
                    break;

                case DownloadStatus.Successful:
                    this.Status = HttpTransferState.Completed;
                    break;
            }
        }
    }
}
