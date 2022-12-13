//using System;
//using System.Collections.Generic;
//using System.Reactive.Linq;

//namespace Shiny.Net.Http;


//class MetricHolder
//{
//    public MetricHolder(string identifier) => this.Identifier = identifier;
//    public string Identifier { get; }
//    public long LastBytesTransferred { get; set; }
//    public DateTime LastPing { get; set; }
//}


//public static class MetricExtensions
//{
//    public static IObservable<HttpTransferMetric> WithMetrics(this IObservable<HttpTransfer> transfers)
//    {
//        var metrics = new Dictionary<string, MetricHolder>();

//        return transfers
//            .Synchronize()
//            .Where(transfer =>
//            {
//                if (transfer.Status == HttpTransferState.InProgress)
//                {
//                    if (transfer.BytesTransferred <= 0 && transfer.FileSize <= 0)
//                        return false;

//                    var holder = Get(transfer, metrics);
//                    var elapsed = DateTime.UtcNow - holder.LastPing;
//                    var totalSeconds = (long)elapsed.TotalSeconds;

//                    return totalSeconds >= 2;
//                }
//                metrics.Remove(transfer.Identifier);
//                return true;
//            })
//            .Select(transfer =>
//            {
//                var result = transfer.Status == HttpTransferState.InProgress
//                    ? Calculate(transfer, metrics)
//                    : new HttpTransferMetric(transfer);

//                return result;
//            });
//    }


//    static MetricHolder Get(HttpTransfer transfer, Dictionary<string, MetricHolder> metrics)
//    {
//        if (!metrics.ContainsKey(transfer.Identifier))
//        {
//            metrics.Add(transfer.Identifier, new MetricHolder(transfer.Identifier)
//            {
//                LastBytesTransferred = transfer.BytesTransferred,
//                LastPing = DateTime.UtcNow
//            });
//        }
//        return metrics[transfer.Identifier];
//    }


//    static HttpTransferMetric Calculate(HttpTransfer transfer, IDictionary<string, MetricHolder> metrics)
//    {
//        var holder = metrics[transfer.Identifier];
//        var elapsed = DateTime.UtcNow - holder.LastPing;
//        var totalSeconds = (long)elapsed.TotalSeconds;

//        var xferDiff = transfer.BytesTransferred - holder.LastBytesTransferred;
//        var bytesPerSecond = xferDiff / totalSeconds;
//        var rawEta = (transfer.FileSize - transfer.BytesTransferred) / bytesPerSecond;
//        var metric = new HttpTransferMetric(transfer, bytesPerSecond, TimeSpan.FromSeconds(rawEta));

//        holder.LastPing = DateTime.UtcNow;
//        holder.LastBytesTransferred = transfer.BytesTransferred;

//        return metric;
//    }
//}
