using System;
using System.Threading.Tasks;
using Shiny.Infrastructure;
using Shiny.Net.Http.Infrastructure;


namespace Shiny.Net.Http
{
    public class HttpClientHttpTransferManager : AbstractHttpTransferManager
    {
        public HttpClientHttpTransferManager(IRepository repository)
        {
        }

        public override Task Cancel(string id)
        {
            throw new NotImplementedException();
        }


        public override IObservable<HttpTransfer> WhenUpdated()
        {
            throw new NotImplementedException();
        }


        //static IObservable<IHttpTransfer> Upload(HttpTransferRequest request)
        //{
        //    this.FileSize = this.Request.LocalFile.Length;
        //    this.RemoteFileName = this.Request.LocalFile.Name;

        //    while (this.Status != HttpTransferState.Completed && !this.cancelSrc.IsCancellationRequested)
        //    {
        //        try
        //        {
        //            var content = new MultipartFormDataContent();
        //            content.Add(
        //                new ProgressStreamContent(
        //                    this.Request.LocalFile.OpenRead(),
        //                    8192,
        //                    sent =>
        //                    {
        //                        this.Status = HttpTransferState.Running;
        //                        this.BytesTransferred += sent;
        //                        this.RunCalculations();
        //                    }
        //                ),
        //                "blob",
        //                this.RemoteFileName
        //            );

        //            await this.httpClient
        //                .PostAsync(this.Request.Uri, content, this.cancelSrc.Token)
        //                .ConfigureAwait(false);

        //            this.Status = this.cancelSrc.IsCancellationRequested
        //                ? HttpTransferState.Cancelled
        //                : HttpTransferState.Completed;
        //        }
        //        catch (TimeoutException)
        //        {
        //            this.Status = HttpTransferState.Retrying;
        //        }
        //        catch (IOException ex)
        //        {
        //            if (ex.InnerException is WebException)
        //            {
        //                this.Status = HttpTransferState.Retrying;
        //            }
        //            else
        //            {
        //                this.Exception = ex;
        //            }
        //        }
        //        catch (WebException ex)
        //        {
        //            switch (ex.Status)
        //            {
        //                case WebExceptionStatus.ConnectFailure:
        //                case WebExceptionStatus.Timeout:
        //                    this.Status = HttpTransferState.Retrying;
        //                    break;

        //                default:
        //                    this.Status = HttpTransferState.Error;
        //                    this.Exception = ex;
        //                    break;
        //            }
        //        }
        //        catch (TaskCanceledException)
        //        {
        //            this.Status = this.cancelSrc.IsCancellationRequested
        //                ? HttpTransferState.Cancelled
        //                : HttpTransferState.Retrying;
        //        }
        //        catch (Exception ex)
        //        {
        //            this.Exception = ex;
        //        }
        //    }
        //}

        //static IObservable<IHttpTransfer> Download(HttpTransferRequest request)
        //{
        //    var file = this.Request.LocalFile;

        //    var fileMode = FileMode.CreateNew;
        //    if (file.Exists)
        //    {
        //        this.ResumeOffset = file.Length + 1;
        //        this.BytesTransferred = file.Length;
        //        fileMode = FileMode.Append;
        //    }

        //    using (var fs = file.Open(fileMode, FileAccess.Write, FileShare.Write))
        //    {
        //        while (this.Status != HttpTransferState.Completed && !this.cancelSrc.IsCancellationRequested)
        //        {
        //            try
        //            {
        //                //this.httpClient.DefaultRequestHeaders.ExpectContinue = false;
        //                this.httpClient.DefaultRequestHeaders.TransferEncodingChunked = true;
        //                this.httpClient.DefaultRequestHeaders.Add("Keep-Alive", "false");

        //                await this.DoDownload(fs);
        //                this.Status = this.cancelSrc.IsCancellationRequested
        //                    ? HttpTransferState.Cancelled
        //                    : HttpTransferState.Completed;
        //            }
        //            catch (TaskCanceledException)
        //            {
        //                this.Status = this.cancelSrc.IsCancellationRequested
        //                    ? HttpTransferState.Cancelled
        //                    : HttpTransferState.Retrying;
        //            }
        //            catch (IOException ex)
        //            {
        //                if (ex.InnerException is WebException)
        //                {
        //                    this.Status = HttpTransferState.Retrying;
        //                }
        //                else
        //                {
        //                    this.Exception = ex;
        //                }
        //            }
        //            catch (Exception ex)
        //            {
        //                this.Exception = ex;
        //            }
        //        }
        //    }
        //}


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
