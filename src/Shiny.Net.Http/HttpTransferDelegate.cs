using System;
using System.Collections.Generic;
using System.Net;
using System.Threading.Tasks;
using Microsoft.Extensions.Logging;

namespace Shiny.Net.Http;


public abstract partial class HttpTransferDelegate(
    ILogger logger, 
    IHttpTransferManager manager,
    int maxErrorRetries = 0
) : IHttpTransferDelegate
{
    protected ILogger Logger => logger;
    protected IHttpTransferManager TransferManager => manager;
    
    const string AuthRetries = nameof(AuthRetries);
    const string ErrorRetries = nameof(ErrorRetries);
    public int MaxErrorRetryAttempts { get; set; } = maxErrorRetries;

    /// <summary>
    /// Create a new HttpTransferRequest with new authorization headers or return null to cancel request
    /// </summary>
    /// <param name="request"></param>
    /// <param name="retries"></param>
    /// <returns></returns>
    protected virtual Task<HttpTransferRequest?> OnAuthorizationFailed(HttpTransferRequest request, int retries) =>
        Task.FromResult((HttpTransferRequest?)null);

    /// <summary>
    /// Mutate the HttpTransferRequest before sending a retry or return null to cancel request
    /// </summary>
    /// <param name="request"></param>
    /// <param name="retries"></param>
    /// <returns></returns>
    protected virtual Task<HttpTransferRequest?> OnBeforeRetry(HttpTransferRequest request, int retries) 
        => Task.FromResult<HttpTransferRequest?>(request);
    
    
    public async Task OnError(HttpTransferRequest request, int statusCode, Exception ex)
    {
        // TODO: auth vs error retries
        var needsAuth = statusCode == (int)HttpStatusCode.Unauthorized;
        var authRetries = 0;
        var errorRetries = 0;
        
        if (request.Headers?.TryGetValue(ErrorRetries, out var retryHeader) ?? false)
            errorRetries = Int32.Parse(retryHeader);

        if (request.Headers?.TryGetValue(AuthRetries, out var authHeader) ?? false)
            authRetries = Int32.Parse(authHeader);
        
        using (logger.BeginScope(new Dictionary<string, string>
        {
            ["RequestId"] = request.Identifier, 
            ["Uri"] = request.Uri,
            ["Method"] = request.HttpMethod!,
            ["Type"] = request.Type.ToString(),
            ["HttpStatusCode"] = statusCode.ToString(),
            ["ErrorRetries"] = errorRetries.ToString(),
            ["AuthRetries"] = authRetries.ToString()
        }))
        {
            if (needsAuth)
            {
                var newRequest = await this.OnAuthorizationFailed(request, authRetries).ConfigureAwait(false);
                if (newRequest == null)
                {
                    logger.LogInformation("Authorization Retry was cancelled after {Attempts} attempts", authRetries);
                }
                else
                {
                    authRetries++;
                    logger.LogInformation("Authorization Retry Attempt: {Attempt}", authRetries);
                    var headers = newRequest.Headers ?? new Dictionary<string, string>();
                    headers[AuthRetries] = authRetries.ToString();
                    newRequest = newRequest with { Headers = headers };
                    await manager.Queue(newRequest).ConfigureAwait(false);
                }
            }
            else
            {
                errorRetries++;
                if (errorRetries >= this.MaxErrorRetryAttempts)
                {
                    logger.LogInformation("Max Errors Exceeded");
                }
                else 
                {
                    
                    var newRequest = await this.OnBeforeRetry(request, errorRetries).ConfigureAwait(false);
                    if (newRequest == null)
                    {
                        logger.LogInformation("Error Retry was cancelled");
                    }
                    else
                    {
                        logger.LogInformation("Error Retry Attempt");
                        var headers = newRequest.Headers ?? new Dictionary<string, string>();
                        headers[ErrorRetries] = errorRetries.ToString();
                        newRequest = newRequest with { Headers = headers };
                        await manager.Queue(newRequest).ConfigureAwait(false);
                    }
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