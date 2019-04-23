using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Jobs;


namespace Shiny.Net.Http
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

            if (request.UseMeteredConnection || this.connectivity.IsDirectConnect())
            {
                var task = request.IsUpload ? this.Upload(request, cancelToken) : this.Download(request, cancelToken);
                var transfer = await task.ConfigureAwait(false);

                switch (transfer.Status)
                {
                    case HttpTransferState.Completed:
                        await this.StopJob(jobInfo);
                        this.tdelegate.OnCompleted(transfer);
                        break;

                    case HttpTransferState.Error:
                        await this.StopJob(jobInfo);
                        this.tdelegate.OnError(transfer, transfer.Exception);
                        break;
                }
            }
            return true;
        }


        HttpRequestMessage Build(HttpTransferStore request)
        {
            var message = new HttpRequestMessage(new HttpMethod(request.HttpMethod), request.Uri);
            //message.Headers.ExpectContinue = false;
            //message.Headers.TransferEncodingChunked = true;
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


        // TODO: pause due to network
        async Task<HttpTransfer> Upload(HttpTransferStore request, CancellationToken ct)
        {
            var file = new FileInfo(request.LocalFile);
            var status = HttpTransferState.Pending;
            //this.RemoteFileName = this.Request.LocalFile.Name;

            // and not cancelled or error
            while (status != HttpTransferState.Completed && !ct.IsCancellationRequested)
            {
                try
                {
                    var bytesTransferred = 0L;

                    var content = new MultipartFormDataContent();
                    content.Add(
                        new ProgressStreamContent(
                            file.OpenRead(),
                            8192,
                            sent =>
                            {
                                status = HttpTransferState.InProgress;
                                bytesTransferred += sent;
                                //this.messageBus.Publish(new HttpTransfer(
                                //));
                                // TODO: trigger
                            }
                        ),
                        "blob",
                        file.Name
                    );
                    var message = this.Build(request);
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
                        //this.Exception = ex;
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
                            //this.Exception = ex;
                            break;
                    }
                }
                catch (TaskCanceledException)
                {
                    status = ct.IsCancellationRequested
                        ? HttpTransferState.Cancelled
                        : HttpTransferState.Retrying;
                }
                catch (Exception ex)
                {
                    //        this.Exception = ex;
                    status = HttpTransferState.Error;
                }

                // TODO: trigger
                //this.messageBus.Publish(new HttpTransfer(
                //));
            }
            //return status;
            return default(HttpTransfer);
        }


        // TODO: pause due to network
        async Task<HttpTransfer> Download(HttpTransferStore request, CancellationToken ct)
        {
            var status = HttpTransferState.Pending;
            var file = new FileInfo(request.LocalFile);
            var fileMode = file.Exists ? FileMode.Append : FileMode.Create;

            using (var fs = file.Open(fileMode, FileAccess.Write, FileShare.Write))
            {
                while (status.IsCompleted() && !ct.IsCancellationRequested)
                {
                    try
                    {
                        //var message = this.Build(request);

                        //await this.DoDownload(fs);
                        //this.Status = this.cancelSrc.IsCancellationRequested
                        //    ? HttpTransferState.Cancelled
                        //    : HttpTransferState.Completed;
                    }
                    catch (TaskCanceledException)
                    {
                        status = ct.IsCancellationRequested
                            ? HttpTransferState.Cancelled
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
                            //this.Exception = ex;
                            status = HttpTransferState.Error;
                        }
                    }
                    catch (Exception ex)
                    {
                        //exception = ex;
                        status = HttpTransferState.Error;
                    }
                }
            }
            return default(HttpTransfer);
        }


        //static bool IsRequestFinished(HttpTransfer transfer)
        //    => transfer.Status.IsCompleted() || transfer.Status.IsPaused();

        async Task DoDownload(HttpTransferStore request, FileStream fs, CancellationToken ct)
        {
            var message = this.Build(request);
            var bytesTransferred = fs.Length;

            if (fs.Length > 0)
            {
                var resumeOffset = fs.Length + 1;
                //this.httpClient.DefaultRequestHeaders.Range = new RangeHeaderValue(this.ResumeOffset, null);
                //message.Content.Headers.ContentRange = new ContentRangeHeaderValue(resumeOffset);
            }

            var buffer = new byte[8192];
            var response = await this.httpClient.SendAsync(
                message,
                HttpCompletionOption.ResponseHeadersRead,
                ct
            );
            response.EnsureSuccessStatusCode();

            var inputStream = await response.Content.ReadAsStreamAsync();
            var read = inputStream.Read(buffer, 0, buffer.Length);

            if (response.Headers.AcceptRanges == null && fs.Length > 0)
            {
                // resume not supported, starting over
                fs.SetLength(0);
                fs.Flush();
                //this.BytesTransferred = 0;
                //this.ResumeOffset = 0;
            }

            while (read > 0 && !ct.IsCancellationRequested)
            {
                //this.FileSize = response.Content.Headers?.ContentRange?.Length ?? response.Content?.Headers?.ContentLength ?? 0;
                //this.RemoteFileName = response.Content?.Headers?.ContentDisposition?.FileName ?? String.Empty;
                ////pr.FileSize = response.Content?.Headers?.ContentLength ?? 0; // this will change on resume
                //this.BytesTransferred += read;
                //status = HttpTransferState.Running;

                //fs.Write(buffer, 0, read);
                //fs.Flush();

                //read = inputStream.Read(buffer, 0, buffer.Length);
            }
        }
    }
}
