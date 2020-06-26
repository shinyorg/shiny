using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Jobs;


namespace Shiny.Net.Http.Infrastructure
{
    public class TransferJob : IJob
    {
        readonly IHttpTransferDelegate tdelegate;
        readonly IConnectivity connectivity;
        readonly IMessageBus messageBus;
        readonly IRepository repository;
        readonly HttpClient httpClient;


        public TransferJob(IHttpTransferDelegate tdelegate,
                           IConnectivity connectivity,
                           IMessageBus messageBus,
                           IRepository repository)
        {
            this.tdelegate = tdelegate;
            this.connectivity = connectivity;
            this.messageBus = messageBus;
            this.repository = repository;
            this.httpClient = new HttpClient();
        }


        public async Task<bool> Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            var request = await this.repository.Get<HttpTransferStore>(jobInfo.Identifier);
            if (request == null)
                return false;

            if (request.UseMeteredConnection || this.connectivity.IsDirectConnect())
            {
                var task = request.IsUpload ? this.Upload(request, cancelToken) : this.Download(request, cancelToken);
                var transfer = await task.ConfigureAwait(false);

                switch (transfer.Status)
                {
                    case HttpTransferState.Completed:
                        await this.StopJob(jobInfo);
                        await this.tdelegate.OnCompleted(transfer);
                        break;

                    case HttpTransferState.Error:
                        if (transfer.Exception == null)
                            return false;

                        await this.StopJob(jobInfo);
                        await this.tdelegate.OnError(transfer, transfer.Exception);
                        break;
                }
            }
            return true;
        }


        HttpRequestMessage Build(HttpTransferStore request)
        {
            var message = new HttpRequestMessage(new HttpMethod(request.HttpMethod), request.Uri);
            //message.Headers.ExpectContinue = false;
            //message.Headers.TransferEncodingChunked = transferEncodingChunked;
            //message.Headers.Add("Keep-Alive", false);

            foreach (var header in request.Headers)
                message.Headers.Add(header.Key, header.Value);

            return message;
        }


        async Task StopJob(JobInfo jobInfo)
        {
            jobInfo.Repeat = false;
            await this.repository.Remove<HttpTransferStore>(jobInfo.Identifier);
        }


        async Task<HttpTransfer> Upload(HttpTransferStore request, CancellationToken ct)
        {
            var file = new FileInfo(request.LocalFile);
            var status = HttpTransferState.Pending;
            var bytesTransferred = 0L;
            Exception? exception = null;
            HttpTransfer lastTransfer = default;

            // and not cancelled or error
            while (!status.IsCompleted() && !ct.IsCancellationRequested)
            {
                try
                {
                    var content = new MultipartFormDataContent();
                    content.Add(
                        new ProgressStreamContent(
                            file.OpenRead(),
                            8192,
                            sent =>
                            {
                                status = HttpTransferState.InProgress;
                                bytesTransferred += sent;
                                lastTransfer = new HttpTransfer(
                                    request.Id,
                                    request.Uri,
                                    request.LocalFile,
                                    true,
                                    request.UseMeteredConnection,
                                    null,
                                    file.Length,
                                    bytesTransferred,
                                    status
                                );
                                this.messageBus.Publish(lastTransfer);
                            }
                        ),
                        "blob",
                        file.Name
                    );
                    var message = this.Build(request);
                    message.Content = content;
                    await this.httpClient
                        .SendAsync(message, ct)
                        .ConfigureAwait(false);

                    status = HttpTransferState.Completed;
                }
                catch (TimeoutException)
                {
                    status = HttpTransferState.Retrying;
                }
                catch (IOException ex)
                {
                    if (ex.InnerException is WebException)
                        status = HttpTransferState.Retrying;
                    else
                    {
                        exception = ex;
                        status = HttpTransferState.Error;
                    }
                }
                catch (WebException ex)
                {
                    switch (ex.Status)
                    {
                        case WebExceptionStatus.ConnectFailure:
                        case WebExceptionStatus.Timeout:
                            status = HttpTransferState.Retrying;
                            break;

                        default:
                            status = HttpTransferState.Error;
                            exception = ex;
                            break;
                    }
                }
                catch (TaskCanceledException)
                {
                    status = ct.IsCancellationRequested
                        ? HttpTransferState.Canceled
                        : HttpTransferState.Retrying;
                }
                catch (Exception ex)
                {
                    exception = ex;
                    status = HttpTransferState.Error;
                }
                lastTransfer = new HttpTransfer(
                    request.Id,
                    request.Uri,
                    request.LocalFile,
                    true,
                    request.UseMeteredConnection,
                    exception,
                    file.Length,
                    bytesTransferred,
                    status
                );
                this.messageBus.Publish(lastTransfer);
            }
            return lastTransfer;
        }


        async Task<HttpTransfer> Download(HttpTransferStore request, CancellationToken ct)
        {
            HttpTransfer lastTransfer = default;
            var status = HttpTransferState.Pending;
            var file = new FileInfo(request.LocalFile);
            var message = this.Build(request);
            var fileSize = 0L;
            var bytesTransferred = file.Exists ? file.Length : 0;
            var fileMode = file.Exists ? FileMode.Append : FileMode.Create;
            Exception? exception = null;

            using (var fs = file.Open(fileMode, FileAccess.Write, FileShare.Write))
            {
                while (!status.IsCompleted() && !ct.IsCancellationRequested)
                {
                    try
                    {
                        if (fs.Length > 0)
                        {
                            var resumeOffset = fs.Length + 1;
                            message.Headers.Range = new RangeHeaderValue(resumeOffset, null);
                        }

                        var buffer = new byte[65535];
                        var response = await this.httpClient
                            .SendAsync(
                                message,
                                HttpCompletionOption.ResponseHeadersRead,
                                ct
                            )
                            .ConfigureAwait(false);


                        response.EnsureSuccessStatusCode();

                        var inputStream = await response
                            .Content
                            .ReadAsStreamAsync()
                            .ConfigureAwait(false);

                        var read = inputStream.Read(buffer, 0, buffer.Length);

                        // status code 206 means restart
                        if (response.Headers.AcceptRanges == null && fs.Length > 0)
                        {
                            // resume not supported, starting over
                            fs.SetLength(0);
                            fs.Flush();
                            bytesTransferred = 0;
                        }

                        var i = 0;
                        while (read > 0 && !ct.IsCancellationRequested)
                        {
                            fileSize = response.Content?.Headers?.ContentRange?.Length ?? response.Content?.Headers?.ContentLength ?? 0;
                            //this.RemoteFileName = response.Content?.Headers?.ContentDisposition?.FileName ?? String.Empty;
                            ////pr.FileSize = response.Content?.Headers?.ContentLength ?? 0; // this will change on resume
                            bytesTransferred += read;

                            fs.Write(buffer, 0, read);
                            fs.Flush();

                            i++;
                            if (i % 4 == 0)
                            {
                                lastTransfer = new HttpTransfer(
                                    request.Id,
                                    request.Uri,
                                    request.LocalFile,
                                    false,
                                    request.UseMeteredConnection,
                                    null,
                                    fileSize,
                                    bytesTransferred,
                                    HttpTransferState.InProgress
                                );
                                this.messageBus.Publish(lastTransfer);
                            }
                            read = inputStream.Read(buffer, 0, buffer.Length);
                        }
                        status = HttpTransferState.Completed;
                    }
                    catch (TaskCanceledException)
                    {
                        status = ct.IsCancellationRequested
                            ? HttpTransferState.Canceled
                            : HttpTransferState.Retrying;
                    }
                    catch (IOException ex)
                    {
                        if (ex.InnerException is WebException)
                        {
                            status = HttpTransferState.Retrying;
                        }
                        else
                        {
                            exception = ex;
                            status = HttpTransferState.Error;
                        }
                    }
                    catch (Exception ex)
                    {
                        exception = ex;
                        status = HttpTransferState.Error;
                    }
                    lastTransfer = new HttpTransfer(
                        request.Id,
                        request.Uri,
                        request.LocalFile,
                        true,
                        request.UseMeteredConnection,
                        exception,
                        fileSize,
                        bytesTransferred,
                        status
                    );
                    this.messageBus.Publish(lastTransfer);
                }
            }
            return lastTransfer;
        }
    }
}
