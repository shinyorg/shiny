using System;
using System.Collections.Generic;
using System.Reactive.Linq;


namespace Shiny.Net.Http
{
    public static class MetricExtensions
    {
        public static IObservable<HttpTransferMetrics> WithMetrics(this IObservable<IHttpTransfer> transfers)
        {
            var startTime = DateTime.UtcNow;
            var metrics = new Dictionary<string, (DateTime LastPing, long LastBytesTransferred)>();

            return transfers.Select(transfer =>
            {
                //var metric = AddOrGet(transfer, metrics);
                //metric.BytesPerSecond = 0;
                //metric.EstimatedCompletionTime = TimeSpan.Zero;

                switch (transfer.Status)
                {
                    case HttpTransferState.InProgress:
                        //ApplyMetric(transfer, metric);
                        break;

                    case HttpTransferState.Cancelled:
                    case HttpTransferState.Completed:
                    case HttpTransferState.Error:
                    default:
                        lock (metrics)
                            metrics.Remove(transfer.Identifier);
                        break;
                }
                //return metric;
                return null;
            });
        }


        static void ApplyMetric(IHttpTransfer transfer, HttpTransferMetrics metric)
        {
            if (transfer.FileSize <= 0 || transfer.BytesTransferred <= 0)
                return;

            // I don't have a start to calc from yet
            if (metric.LastChanged == null)
            {
                metric.LastChanged = DateTime.UtcNow;
                metric.LastBytesTransferred = transfer.BytesTransferred;
            }
            else
            {
                var elapsed = DateTime.UtcNow - metric.LastChanged.Value;
                var totalSeconds = (long)elapsed.TotalSeconds;

                // sampling rate of 2 seconds
                if (totalSeconds >= 2)
                {
                    var xferDiff = transfer.BytesTransferred - metric.LastBytesTransferred;
                    var bytesPerSecond = xferDiff / totalSeconds;
                    var rawEta = (transfer.FileSize - transfer.BytesTransferred) / bytesPerSecond;

                    //Console.WriteLine($"b/s: {bytesPerSecond} - ETA: {rawEta}");
                    //metric.BytesPerSecond = Convert.ToInt64(bytesPerSecond);
                    //metric.EstimatedCompletionTime = TimeSpan.FromSeconds(rawEta);
                    metric.LastChanged = DateTime.UtcNow;
                    metric.LastBytesTransferred = transfer.BytesTransferred;
                }
            }
        }


        //static HttpTransferMetrics AddOrGet(IHttpTransfer transfer, IDictionary<string, HttpTransferMetrics> metrics)
        //{
        //    lock (metrics)
        //    {
        //        HttpTransferMetrics metric = null;
        //        if (metrics.ContainsKey(transfer.Identifier))
        //            metric = metrics[transfer.Identifier];
        //        else
        //        {
        //            metric = new HttpTransferMetrics { Transfer = transfer };
        //            metrics.Add(transfer.Identifier, metric);
        //        }
        //        return metric;
        //    }
        //}

    }
}
/*
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
        }*/
