using Microsoft.Extensions.Logging;
using Shiny.Net.Http;

namespace Sample.Linux.Delegates;


public class SampleHttpTransferDelegate(
    ILogger<SampleHttpTransferDelegate> logger,
    IHttpTransferManager manager
) : HttpTransferDelegate(logger, manager, maxErrorRetries: 3)
{
    public override Task OnCompleted(HttpTransferRequest request)
    {
        logger.LogInformation("Transfer completed: {Id}", request.Identifier);
        return Task.CompletedTask;
    }
}
