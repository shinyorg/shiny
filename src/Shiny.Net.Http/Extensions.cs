using System;
using System.Collections.Generic;
using System.Reactive.Linq;


namespace Shiny.Net.Http
{
    public static class Extensions
    {
        public static IObservable<HttpTransferMetrics> WithMetrics(this IObservable<IHttpTransfer> transfers)
        {
            var startTime = DateTime.UtcNow;
            var metrics = new Dictionary<string, HttpTransferMetrics>();

            return transfers.Select(transfer =>
            {
                var metric = AddOrGet(transfer, metrics);
                metric.BytesPerSecond = 0;
                metric.EstimatedCompletionTime = TimeSpan.Zero;

                switch (transfer.Status)
                {
                    case HttpTransferState.InProgress:
                        ApplyMetric(transfer, metric);
                        break;

                    case HttpTransferState.Cancelled:
                    case HttpTransferState.Completed:
                    case HttpTransferState.Error:
                        Remove(transfer, metrics);
                        break;

                    default:
                        metric.LastChanged = null;
                        metric.LastBytesTransferred = 0;
                        break;
                }
                return metric;
            });
        }


        static void ApplyMetric(IHttpTransfer transfer, HttpTransferMetrics metric)
        {
            if (transfer.FileSize <= 0 || transfer.BytesTransferred <= 0)
                return;

            if (metric.LastChanged == null)
            {
                metric.LastChanged = DateTime.UtcNow;
                metric.LastBytesTransferred = transfer.BytesTransferred;
                return;
            }
            var elapsed = DateTime.UtcNow - metric.LastChanged.Value;
            metric.BytesPerSecond = (metric.LastBytesTransferred - transfer.BytesTransferred) / (long)elapsed.TotalSeconds;

            var rawEta = transfer.FileSize / metric.BytesPerSecond;
            metric.EstimatedCompletionTime = TimeSpan.FromSeconds(rawEta);

            metric.LastBytesTransferred = transfer.BytesTransferred;
            metric.LastChanged = DateTime.UtcNow;
        }


        static HttpTransferMetrics AddOrGet(IHttpTransfer transfer, IDictionary<string, HttpTransferMetrics> metrics)
        {
            lock (metrics)
            {
                HttpTransferMetrics metric = null;
                if (!metrics.ContainsKey(transfer.Identifier))
                {
                    metric = new HttpTransferMetrics { Transfer = transfer };
                    metrics.Add(transfer.Identifier, metric);
                }
                return metric;
            }
        }


        static void Remove(IHttpTransfer transfer, IDictionary<string, HttpTransferMetrics> metrics)
        {
            lock (metrics)
                metrics.Remove(transfer.Identifier);
        }
    }
}