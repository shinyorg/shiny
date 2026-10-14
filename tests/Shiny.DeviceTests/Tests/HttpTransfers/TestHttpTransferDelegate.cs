using Shiny.Net.Http;

namespace Shiny.DeviceTests.Tests;


public record HttpTransferFailure(HttpTransferRequest Request, int StatusCode, Exception Exception);


// a plain IHttpTransferDelegate (not HttpTransferDelegate) - its retry logic would re-queue the failures the suite provokes
public partial class TestHttpTransferDelegate(EventHub events) : IHttpTransferDelegate
{
    public const string CompletedTopic = "http.completed";
    public const string ErrorTopic = "http.error";

    public Task OnCompleted(HttpTransferRequest request)
    {
        events.Publish(CompletedTopic, request);
        return Task.CompletedTask;
    }

    public Task OnError(HttpTransferRequest request, int statusCode, Exception ex)
    {
        events.Publish(ErrorTopic, new HttpTransferFailure(request, statusCode, ex));
        return Task.CompletedTask;
    }
}


#if ANDROID
public partial class TestHttpTransferDelegate : IAndroidForegroundServiceDelegate
{
    public void Configure(AndroidX.Core.App.NotificationCompat.Builder builder)
    {
        builder.SetContentTitle("Shiny Device Tests");
        builder.SetContentText("Running HTTP transfer tests");
    }
}
#endif
