using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shiny.Net.Http;


public abstract partial class HttpTransferDelegate(
    ILogger logger, 
    IHttpTransferManager manager,
    int maxRetries = 0
) : IHttpTransferDelegate
{
    protected ILogger Logger => logger;
    protected IHttpTransferManager TransferManager => manager;
    
    const string RetryHeader = "Retries";
    public int MaxRetryAttempts { get; set; } = maxRetries;

    // return null if cancelling
    protected virtual Task<HttpTransferRequest?> OnAuthorizationFailed(HttpTransferRequest request, int retries) =>
        Task.FromResult((HttpTransferRequest?)null);

    protected virtual Task<HttpTransferRequest?> OnBeforeRetry(HttpTransferRequest request, int retries) 
        => Task.FromResult(request);

    
    protected virtual Task<bool> ShouldRetry(HttpTransferRequest request, int statusCode, int retries)
    {
        var needsAuth = statusCode == (int)HttpStatusCode.Unauthorized;
        return Task.FromResult(retries <= this.MaxRetryAttempts || needsAuth);
    }
    
    
    public async Task OnError(HttpTransferRequest request, int statusCode, Exception ex)
    {
        
        var needsAuth = statusCode == (int)HttpStatusCode.Unauthorized;
        var retries = 0;
        if (request.Headers?.TryGetValue(RetryHeader, out var retryHeader) ?? false)
        {
            retries = Int32.Parse(retryHeader);
        }
        
        using (logger.BeginScope(new Dictionary<string, string>
        {
            ["RequestId"] = request.Identifier, 
            ["Uri"] = request.Uri,
            ["Method"] = request.HttpMethod!,
            ["Type"] = request.Type.ToString(),
            ["HttpStatusCode"] = statusCode.ToString(),
            ["RetryCount"] = retries.ToString()
        }))
        {
            var shouldRetry = await this.ShouldRetry(request, statusCode, retries).ConfigureAwait(false);
            if (!shouldRetry)
            {
                logger.LogDebug("User cancelled http transfer.");
                return;
            }

            if (needsAuth)
            {
                // does not count as a retry
                request = await this.OnAuthorizationFailed(request, retries).ConfigureAwait(false);
                if (request == null)
                {
                    logger.LogDebug("Cancelling Retry from Auth");
                }
                else
                {
                    logger.LogDebug(ex, "Requeue Auth Failure Transfer");
                }
            }
            retries++;
            var headers = request!.Headers ?? new Dictionary<string, string>();
            headers[RetryHeader] = retries.ToString();
            request = request with { Headers = headers };
            request = await this.OnBeforeRetry(request, retries).ConfigureAwait(false);
            if (request != null)
                await manager.Queue(request).ConfigureAwait(false);
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