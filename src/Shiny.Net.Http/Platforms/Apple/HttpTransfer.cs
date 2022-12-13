using System;
using System.Threading.Tasks;
using Foundation;

namespace Shiny.Net.Http;


public class HttpTransfer : NotifyPropertyChanged, IHttpTransfer
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

    public HttpTransferState Status => throw new NotImplementedException();
    public long BytesTransferred => throw new NotImplementedException();
    public long BytesToTransfer => throw new NotImplementedException();

    public Task Cancel()
    {
        this.downloadTask?.Cancel();
        this.uploadTask?.Cancel();



        //this.uploadTask.State
        //var task = this.session.CreateDownloadTask(null);
        return Task.CompletedTask;
    }


    public Task Pause() => throw new NotImplementedException();
    public Task Resume() => throw new NotImplementedException();
}