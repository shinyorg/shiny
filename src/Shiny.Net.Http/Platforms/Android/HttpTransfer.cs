using System;
using System.IO;
using System.Net.Http;
using System.Threading;
using System.Threading.Tasks;

namespace Shiny.Net.Http;


public class HttpTransfer : NotifyPropertyChanged, IHttpTransfer
{
    CancellationTokenSource? cts;
    readonly HttpClient httpClient = new();
    readonly IServiceProvider services;


    public HttpTransfer(
        IServiceProvider services,
        HttpTransferRequest request,
        string identifier
    )
    {
        this.services = services;
        this.Request = request;
        this.Identifier = identifier;
    }


    public string Identifier { get; }
    public HttpTransferRequest Request { get; }


    HttpTransferState status = HttpTransferState.Pending;
    public HttpTransferState Status
    {
        get => this.status;
        private set => this.Set(ref this.status, value);
    }

    
    long bytesXfer;
    public long BytesTransferred
    {
        get => this.bytesXfer;
        private set => this.Set(ref this.bytesXfer, value);
    }


    long bytesToXfer;
    public long BytesToTransfer
    {
        get => this.bytesToXfer;
        private set => this.Set(ref this.bytesToXfer, value);
    }


    public Task Cancel()
    {
        this.Status = HttpTransferState.Canceled;
        this.cts?.Cancel();
        return Task.CompletedTask;
    }


    public Task Pause()
    {
        this.Status = HttpTransferState.Paused;
        this.cts?.Cancel();
        return Task.CompletedTask;
    }


    public async Task Resume()
    {
        this.Status = HttpTransferState.InProgress;
        if (this.Request.IsUpload)
            await this.Upload();
        else
            await this.Download();
    }


    async Task Download()
    {
        this.cts = new();
        var buffer = new byte[8192];
        var request = this.GetRequest();

        try
        {
            // TODO: could try resume download instead of recreating file?
            // TODO: write to cache instead & move after?
            using var localFile = File.OpenWrite(this.Request.LocalFile.FullName);

            using var response = await this.httpClient
                .SendAsync(request, this.cts.Token)
                .ConfigureAwait(false);

            response.EnsureSuccessStatusCode();
            using var stream = response.Content.ReadAsStream(this.cts.Token);

            this.BytesToTransfer = response.Content.Headers.ContentLength ?? 0;
            var read = stream.Read(buffer, 0, buffer.Length);

            while (read > 0 && !this.cts.IsCancellationRequested)
            {
                read = stream.Read(buffer, 0, buffer.Length);
                localFile.Write(buffer, 0, buffer.Length);
                this.BytesTransferred += read;
            }

            this.Status = HttpTransferState.Completed;
            await this.services.RunDelegates<IHttpTransferDelegate>(x => x.OnCompleted(this));
        }
        catch (TaskCanceledException) { }
        catch (HttpRequestException ex)
        {
            this.Status = HttpTransferState.Retrying; // TODO: or pending
            //ex.StatusCode.
        }
        catch (Exception ex)
        {
            this.Status = HttpTransferState.Error;
            await this.services.RunDelegates<IHttpTransferDelegate>(x => x.OnError(this, ex));
        }
    }


    async Task Upload()
    {
        this.cts = new();

        try
        {
            var progress = new Action<int>(sent => this.BytesTransferred += sent);

            var request = this.GetRequest();
            request.Content = new ProgressStreamContent(this.Request.LocalFile.OpenRead(), progress, 8192);

            var response = await this.httpClient.SendAsync(request).ConfigureAwait(false);
            response.EnsureSuccessStatusCode();

            // TODO: on complete
        }
        catch (TaskCanceledException) { }
        catch (HttpRequestException ex)
        {
        }
        catch (Exception ex)
        {
            this.Status = HttpTransferState.Error;
        }
    }


    HttpRequestMessage GetRequest()
    {
        var request = new HttpRequestMessage(
            this.Request.HttpMethod ?? HttpMethod.Get,
            this.Request.Uri
        );

        if (this.Request.Headers != null)
            foreach (var header in this.Request.Headers)
                request.Headers.TryAddWithoutValidation(header.Key, header.Value);

        if (!this.Request.PostData.IsEmpty())
            request.Content = new StringContent(this.Request.PostData!);

        return request;
    }
}