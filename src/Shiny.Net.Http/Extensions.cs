using System;
using System.Linq;
using System.Reactive.Linq;

namespace Shiny.Net.Http;


public static class Extensions
{
    public static double PercentComplete(this IHttpTransfer transfer)
    {

        //if (this.BytesTransferred <= 0 || this.FileSize <= 0)
        //    return 0;

        //var raw = ((double)this.BytesTransferred / (double)this.FileSize);
        //return Math.Round(raw, 2);
        return 0;
    }


    public static IObservable<(TimeSpan EstimateTimeRemaining, double BytesPerSecond)> WatchMetrics(this IHttpTransfer transfer) =>
        transfer
            .WhenAnyProperty()
            .Select(x => (x.Object.BytesTransferred, x.Object.BytesToTransfer))
            .Buffer(TimeSpan.FromSeconds(2))
            .Select(results =>
            {
                var timeRemaining = TimeSpan.Zero;
                double bytesPerSecond = 0;

                if (results.Count > 0)
                {
                    // total bytes to transfer - all bytes transferred = delta
                    // add all deltas over the past 2 seconds for total bytes xfer
                    var totalBytes = results.Sum(x => x.BytesToTransfer - x.BytesTransferred);
                    bytesPerSecond = (double)totalBytes / 2; // in two seconds

                    var latest = results.Last();
                    var remainingBytes = latest.BytesToTransfer - latest.BytesTransferred;
                    var secondsRemaining = remainingBytes / bytesPerSecond;
                    timeRemaining = TimeSpan.FromSeconds(secondsRemaining);
                }
                return (timeRemaining, bytesPerSecond);
            });


    public static bool IsCompleted(this HttpTransferState status) => status switch
    {
        HttpTransferState.Completed => true,
        HttpTransferState.Error => true,
        HttpTransferState.Canceled => true,
        _ => false
    };


    public static bool IsPaused(this HttpTransferState status) => status switch
    {
        HttpTransferState.Paused => true,
        HttpTransferState.PausedByCostedNetwork => true,
        HttpTransferState.PausedByNoNetwork => true,
        _ => false
    };
}