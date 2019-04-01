using System;
using System.IO;
using System.Net;
using System.Net.Http;
using System.Net.Http.Headers;
using System.Threading;
using System.Threading.Tasks;


namespace Shiny.Net.Http
{

    public class UploadHttpTransfer : AbstractHttpTransfer
    {
        readonly HttpClient httpClient;
        readonly CancellationTokenSource cancelSrc;


        public UploadHttpTransfer(HttpTransferRequest request, string identifier) : base(request, false)
        {
            this.Identifier = identifier;

            this.httpClient = new HttpClient();
            this.cancelSrc = new CancellationTokenSource();
            this.Identifier = identifier;
            this.DoUpload();
        }


        //public override void Cancel()
        //{
        //    this.Status = HttpTransferState.Cancelled;
        //    this.cancelSrc.Cancel();
        //    this.httpClient.CancelPendingRequests();
        //}


        async Task DoUpload()
        {
            this.FileSize = this.Request.LocalFile.Length;
            this.RemoteFileName = this.Request.LocalFile.Name;

            while (this.Status != HttpTransferState.Completed && !this.cancelSrc.IsCancellationRequested)
            {
                try
                {
                    var content = new MultipartFormDataContent();
                    content.Add(
                        new ProgressStreamContent(
                            this.Request.LocalFile.OpenRead(),
                            8192,
                            sent =>
                            {
                                this.Status = HttpTransferState.Running;
                                this.BytesTransferred += sent;
                                this.RunCalculations();
                            }
                        ),
                        "blob",
                        this.RemoteFileName
                    );

                    await this.httpClient
                        .PostAsync(this.Request.Uri, content, this.cancelSrc.Token)
                        .ConfigureAwait(false);

                    this.Status = this.cancelSrc.IsCancellationRequested
                        ? HttpTransferState.Cancelled
                        : HttpTransferState.Completed;
                }
                catch (TimeoutException)
                {
                    this.Status = HttpTransferState.Retrying;
                }
                catch (IOException ex)
                {
                    if (ex.InnerException is WebException)
                    {
                        this.Status = HttpTransferState.Retrying;
                    }
                    else
                    {
                        this.Exception = ex;
                    }
                }
                catch (WebException ex)
                {
                    switch (ex.Status)
                    {
                        case WebExceptionStatus.ConnectFailure:
                        case WebExceptionStatus.Timeout:
                            this.Status = HttpTransferState.Retrying;
                            break;

                        default:
                            this.Status = HttpTransferState.Error;
                            this.Exception = ex;
                            break;
                    }
                }
                catch (TaskCanceledException)
                {
                    this.Status = this.cancelSrc.IsCancellationRequested
                        ? HttpTransferState.Cancelled
                        : HttpTransferState.Retrying;
                }
                catch (Exception ex)
                {
                    this.Exception = ex;
                }
            }
        }
    }
}
