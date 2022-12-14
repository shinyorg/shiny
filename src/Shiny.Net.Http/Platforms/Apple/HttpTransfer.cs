using System;
using System.Reactive.Linq;
using Foundation;

namespace Shiny.Net.Http;


public class HttpTransfer : IHttpTransfer
{
    public HttpTransfer(HttpTransferRequest request, NSUrlSessionTask task)
    {
        this.Request = request;
        this.NSTask = task;
    }


    public NSUrlSessionTask NSTask { get; }
    public string Identifier => this.NSTask.TaskIdentifier.ToString();
    public HttpTransferRequest Request { get; }

    public HttpTransferState Status => this.NSTask.GetStatus();
    public long BytesTransferred => this.Request.IsUpload
        ? this.NSTask.BytesExpectedToSend
        : this.NSTask.BytesExpectedToReceive;

    public long BytesToTransfer => this.Request.IsUpload
        ? this.NSTask.BytesSent
        : this.NSTask.BytesReceived;


    public double PercentComplete => this.NSTask.Progress.FractionCompleted;


    public IObservable<HttpTransferMetrics> ListenToMetrics() => Observable
        .Interval(TimeSpan.FromSeconds(2))
        .Select(x =>
        {
            // TODO: return null if indeterminate
            //this.NSTask.Progress.Indeterminate
            //this.NSTask.Progress.Throughput
            var ts = TimeSpan.FromSeconds((int)this.NSTask.Progress.EstimatedTimeRemaining!);
            var bps = (long)this.NSTask.Progress.Throughput!;
            return new HttpTransferMetrics(
                ts,
                bps,
                this.BytesToTransfer,
                this.BytesTransferred,
                this.PercentComplete,
                this.Status
            );
        });


    
    internal void Cancel() => this.NSTask.Cancel();

    // TODO: uploads cannot be paused/resumed
    internal void Pause() => this.NSTask.Suspend();
    internal void Resume() => this.NSTask.Resume();
}