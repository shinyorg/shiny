using Microsoft.Extensions.Logging;
using Shiny.Net.Http;

namespace Sample.Maui.Delegates;

public class SampleHttpTransferDelegate(ILogger<SampleHttpTransferDelegate> logger) : IHttpTransferDelegate
{
    public Task OnError(HttpTransferRequest request, int statusCode, Exception ex)
    {
        logger.LogError(ex, "HTTP Transfer Error: {Uri} - Status {StatusCode}", request.Uri, statusCode);
        return Task.CompletedTask;
    }

    public Task OnCompleted(HttpTransferRequest request)
    {
        logger.LogInformation("HTTP Transfer Completed: {Uri}", request.Uri);
        return Task.CompletedTask;
    }
}
