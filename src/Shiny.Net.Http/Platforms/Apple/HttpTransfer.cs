using System;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
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

    internal Subject<long> OnBytesTransferred { get; } = new();
    public IObservable<HttpTransferMetrics> ListenToMetrics() => this.OnBytesTransferred
        .Buffer(TimeSpan.FromSeconds(2))
        .Select(results =>
        {
            var timeRemaining = TimeSpan.Zero;
            var bytesPerSecond = 0L;
            if (results.Count > 0)
            {
                var totalBytes = results.Sum();
                bytesPerSecond = Convert.ToInt64((double)totalBytes / 2);

                var secondsRemaining = (this.BytesToTransfer - this.BytesTransferred) / bytesPerSecond;
                timeRemaining = TimeSpan.FromSeconds(secondsRemaining);
            }
            return new HttpTransferMetrics(
                timeRemaining,
                bytesPerSecond,
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