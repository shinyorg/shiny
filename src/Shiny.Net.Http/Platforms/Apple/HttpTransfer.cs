using System;
using Foundation;

namespace Shiny.Net.Http;


public class HttpTransfer : IHttpTransfer
{
    NSUrlSessionDownloadTask? downloadTask;
    NSUrlSessionUploadTask? uploadTask;

    public HttpTransfer(HttpTransferRequest request, NSUrlSessionUploadTask uploadTask)
    {
        this.Request = request;
        this.uploadTask = uploadTask;
    }


    public HttpTransfer(HttpTransferRequest request, NSUrlSessionDownloadTask downloadTask)
    {
        this.Request = request;
        this.downloadTask = downloadTask;
    }


    public string Identifier => this.downloadTask?.TaskDescription! ?? this.uploadTask?.TaskDescription!;
    public HttpTransferRequest Request { get; }

    public HttpTransferState Status => HttpTransferState.Unknown;// (this.uploadTask ?? this.downloadTask).GetStatus();
    public ulong BytesTransferred => throw new NotImplementedException();
    public ulong BytesPerSeconds => throw new NotImplementedException();
    public TimeSpan EstimateTimeRemaining => throw new NotImplementedException();

    public void Cancel()
    {
        this.downloadTask?.Cancel();
        this.uploadTask?.Cancel();



        //this.uploadTask.State
        //var task = this.session.CreateDownloadTask(null);
    }


    public void Pause() => throw new NotImplementedException();
    public void Resume() => throw new NotImplementedException();
}