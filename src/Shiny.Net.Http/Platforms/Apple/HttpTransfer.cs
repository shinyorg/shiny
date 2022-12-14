using System;
using System.Reactive.Linq;
using System.Threading.Tasks;
using Foundation;

namespace Shiny.Net.Http;


public class HttpTransfer : NotifyPropertyChanged, IHttpTransfer
{
    public HttpTransfer(HttpTransferRequest request, NSUrlSessionTask task)
    {
        this.Request = request;
        this.NSTask = task;
    }


    public NSUrlSessionTask NSTask { get; }
    public string Identifier => this.NSTask.TaskDescription!;
    public HttpTransferRequest Request { get; }

    public HttpTransferState Status => this.NSTask.GetStatus();
    public long BytesTransferred => this.Request.IsUpload
        ? this.NSTask.BytesExpectedToSend
        : this.NSTask.BytesExpectedToReceive;

    public long BytesToTransfer => this.Request.IsUpload
        ? this.NSTask.BytesSent
        : this.NSTask.BytesReceived;


    public double PercentComplete => this.NSTask.Progress.FractionCompleted;

    public IObservable<(TimeSpan EstimateTimeRemaining, double BytesPerSecond)> WatchMetrics() => Observable
        .Interval(TimeSpan.FromSeconds(2))
        .Select(x =>
        {
            // TODO: return null if indeterminate
            //this.NSTask.Progress.Indeterminate
            //this.NSTask.Progress.Throughput
            var ts = TimeSpan.FromSeconds((int)this.NSTask.Progress.EstimatedTimeRemaining!);
            var bps = (double)this.NSTask.Progress.Throughput; // TODO: should be long
            return (ts, bps);
        });


    internal void Cancel()
    {
        // TODO: uploads cannot be paused
        this.NSTask.Cancel();
        this.RaisePropertyChanged(nameof(this.Status));
    }


    internal void Pause()
    {
        // TODO: uploads cannot be paused
        this.NSTask.Suspend();
        this.RaisePropertyChanged(nameof(this.Status));
    }


    internal void Resume()
    {
        this.NSTask.Resume();
        this.RaisePropertyChanged(nameof(this.Status));
    }
}