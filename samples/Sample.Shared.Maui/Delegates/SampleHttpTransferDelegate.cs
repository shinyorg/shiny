using Microsoft.Extensions.Logging;
using Sample.Shared.Maui.Services;
using Shiny.Net.Http;

namespace Sample.Shared.Maui.Delegates;

public class SampleHttpTransferDelegate(
    ILogger<SampleHttpTransferDelegate> logger,
    IEventStore events
) : IHttpTransferDelegate
{
    public Task OnError(HttpTransferRequest request, int statusCode, Exception ex)
    {
        logger.LogError(ex, "HTTP Transfer Error: {Uri} - Status {StatusCode}", request.Uri, statusCode);
        return events.Add(
            "HttpTransfer",
            $"Error {statusCode}: {request.Identifier}",
            new Dictionary<string, string?>
            {
                ["Identifier"] = request.Identifier,
                ["Uri"] = request.Uri,
                ["Type"] = request.Type.ToString(),
                ["StatusCode"] = statusCode.ToString(),
                ["Exception"] = ex.GetType().Name,
                ["Message"] = ex.Message
            }
        );
    }

    public Task OnCompleted(HttpTransferRequest request)
    {
        logger.LogInformation("HTTP Transfer Completed: {Uri}", request.Uri);
        return events.Add(
            "HttpTransfer",
            $"Completed: {request.Identifier}",
            new Dictionary<string, string?>
            {
                ["Identifier"] = request.Identifier,
                ["Uri"] = request.Uri,
                ["Type"] = request.Type.ToString(),
                ["LocalFilePath"] = request.LocalFilePath
            }
        );
    }
}
