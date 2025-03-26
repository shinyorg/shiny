using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shiny.Net.Http;


public abstract partial class HttpTransferDelegate(ILogger logger, IHttpTransferManager manager) : IHttpTransferDelegate
{
    const string RetryHeader = "Retries";
    public int MaxRetryAttempts { get; set; } = 0;

    // return null if cancelling
    protected virtual Task<HttpTransferRequest?> OnAuthorizationFailed(HttpTransferRequest request, int retries) =>
        Task.FromResult((HttpTransferRequest?)null);

    protected virtual Task<HttpTransferRequest?> OnBeforeRetry(HttpTransferRequest request, int retries) 
        => Task.FromResult(request);
    
    public async Task OnError(HttpTransferRequest request, int statusCode, Exception ex)
    {
        var needsAuth = statusCode == (int)HttpStatusCode.Unauthorized;
        
        using (logger.BeginScope(new Dictionary<string, string>
        {
            ["RequestId"] = request.Identifier,
            ["Uri"] = request.Uri,
            ["Method"] = request.HttpMethod!,
            ["Type"] = request.Type.ToString(),
        }))
        {
            var retries = 0;
            if (request.Headers?.TryGetValue(RetryHeader, out var retryHeader) ?? false)
            {
                retries = Int32.Parse(retryHeader);
            }
            
            if (needsAuth)
            {
                // does not count as a retry
                var newRequest = await this.OnAuthorizationFailed(request, retries).ConfigureAwait(false);
                if (newRequest == null)
                {
                    logger.LogDebug("Cancelling Retry from Auth");
                }
                else
                {
                    logger.LogDebug(ex, "Requeue Auth Failure Transfer");
                    await manager.Queue(newRequest).ConfigureAwait(false);
                }
            }
            else if (this.MaxRetryAttempts > 0)
            {
                if (retries < this.MaxRetryAttempts)
                {
                    retries++;
                    var headers = request.Headers ?? new Dictionary<string, string>();
                    headers[RetryHeader] = retries.ToString();

                    var updatedRequest = request with { Headers = headers };
                    var newRequest = await this.OnBeforeRetry(updatedRequest, retries).ConfigureAwait(false);
                    if (newRequest != null)
                        await manager.Queue(newRequest).ConfigureAwait(false);
                }
                else
                {
                    logger.LogWarning("Max Retries for transfer");
                }
            }
        };
    }
    
    public virtual Task OnCompleted(HttpTransferRequest request) => Task.CompletedTask;
}

#if ANDROID
public abstract partial class HttpTransferDelegate : IAndroidForegroundServiceDelegate
{
    public abstract void Configure(AndroidX.Core.App.NotificationCompat.Builder builder);
}
#endif