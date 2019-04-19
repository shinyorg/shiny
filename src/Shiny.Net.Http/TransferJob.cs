using System;
using System.IO;
using System.Net;
using System.Net.Http;
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


        public TransferJob(IHttpTransferDelegate tdelegate,
                           IConnectivity connectivity,
                           IMessageBus messageBus,
                           IRepository repository)
        {
            this.tdelegate = tdelegate;
            this.connectivity = connectivity;
            this.messageBus = messageBus;
            this.repository = repository;
        }


        public async Task<bool> Run(JobInfo jobInfo, CancellationToken cancelToken)
        {
            var request = await this.repository.Get<HttpTransferStore>(jobInfo.Identifier);

            // TODO: once transfer is over, kill repeat and delete store
            if (request.UseMeteredConnection || this.connectivity.IsDirectConnect())
            {
                //var task = await this.Download(request, cancelToken).ConfigureAwait(false);

                //switch (status)
                //{
                //    case HttpTransferState.Completed:
                //        await this.StopJob(jobInfo);
                //        //this.tdelegate.OnCompleted(null);
                //        break;

                //    case HttpTransferState.Error:
                //        await this.StopJob(jobInfo);
                //        //this.tdelegate.OnError(null, null);
                //        break;
                //}
            }
            return true;
        }


        async Task StopJob(JobInfo jobInfo)
        {
            jobInfo.Repeat = false;
            await this.repository.Remove<HttpTransferStore>(jobInfo.Identifier);
        }


        async Task<HttpTransferState> Upload(HttpTransferStore request, CancellationToken ct)
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
                                // TODO: trigger
                            }
                        ),
                        "blob",
                        file.Name
                    );

                    //await httpClient
                    //    .PostAsync(request.Uri, content, ct)
                    //    .ConfigureAwait(false);

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
            }
            return status;
        }


        async Task Download(HttpTransferStore request, CancellationToken ct)
        {
            var file = new FileInfo(request.LocalFile);

            var fileMode = FileMode.CreateNew;
            var resumeOffset = 0L;
            var bytesTransferred = file.Length;
            var status = HttpTransferState.Unknown;

            if (file.Exists)
            {
                resumeOffset = file.Length + 1;
                bytesTransferred = file.Length;
                fileMode = FileMode.Append;
            }

            using (var fs = file.Open(fileMode, FileAccess.Write, FileShare.Write))
            {
                // and not cancelled or error
                while (status != HttpTransferState.Completed && !ct.IsCancellationRequested)
                {
                    try
                    {
                        //this.httpClient.DefaultRequestHeaders.ExpectContinue = false;
                        //this.httpClient.DefaultRequestHeaders.TransferEncodingChunked = true;
                        //this.httpClient.DefaultRequestHeaders.Add("Keep-Alive", "false");

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
        }


        //static async Task DoDownload(HttpTransferRequest request, FileStream fs)
        //{
        //    var buffer = new byte[8192];
        //    if (this.BytesTransferred > 0)
        //    {
        //        this.ResumeOffset = this.BytesTransferred + 1;
        //        this.httpClient.DefaultRequestHeaders.Range = new RangeHeaderValue(this.ResumeOffset, null);
        //    }
        //    var response = await this.httpClient.GetAsync(
        //        this.Request.Uri,
        //        HttpCompletionOption.ResponseHeadersRead,
        //        this.cancelSrc.Token
        //    );
        //    response.EnsureSuccessStatusCode();

        //    var inputStream = await response.Content.ReadAsStreamAsync();
        //    var read = inputStream.Read(buffer, 0, buffer.Length);

        //    if (response.Headers.AcceptRanges == null && fs.Length > 0)
        //    {
        //        // resume not supported, starting over
        //        fs.SetLength(0);
        //        fs.Flush();
        //        this.BytesTransferred = 0;
        //        this.ResumeOffset = 0;
        //    }

        //    while (read > 0 && !this.cancelSrc.IsCancellationRequested)
        //    {
        //        this.FileSize = response.Content.Headers?.ContentRange?.Length ?? response.Content?.Headers?.ContentLength ?? 0;
        //        this.RemoteFileName = response.Content?.Headers?.ContentDisposition?.FileName ?? String.Empty;
        //        //pr.FileSize = response.Content?.Headers?.ContentLength ?? 0; // this will change on resume
        //        this.BytesTransferred += read;
        //        this.Status = HttpTransferState.Running;

        //        fs.Write(buffer, 0, read);
        //        fs.Flush();

        //        this.RunCalculations();

        //        read = inputStream.Read(buffer, 0, buffer.Length);
        //    }
        //}
    }
}
