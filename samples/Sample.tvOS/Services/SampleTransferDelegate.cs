using Shiny.Net.Http;

namespace Sample.tvOS.Services;


public class SampleTransferDelegate(AppLog log) : IHttpTransferDelegate
{
    public Task OnCompleted(HttpTransferRequest request)
    {
        log.Write($"transfer completed: {request.Identifier} -> {request.LocalFilePath}");
        return Task.CompletedTask;
    }

    public Task OnError(HttpTransferRequest request, int statusCode, Exception ex)
    {
        log.Write($"transfer failed ({statusCode}): {request.Identifier} - {ex.Message}");
        return Task.CompletedTask;
    }
}
