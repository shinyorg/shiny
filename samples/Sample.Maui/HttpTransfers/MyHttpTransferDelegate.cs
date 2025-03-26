using Shiny.Net.Http;
using Shiny.Notifications;

namespace Sample.HttpTransfers;


public partial class MyHttpTransferDelegate(
    INotificationManager notificationManager, 
    SampleSqliteConnection conn
) : IHttpTransferDelegate
{
    public Task OnError(HttpTransferRequest request, int statusCode, Exception ex)
        => this.CreateHttpTransferEvent(request, ex);


    public Task OnCompleted(HttpTransferRequest request)
        => this.CreateHttpTransferEvent(request);


    async Task CreateHttpTransferEvent(HttpTransferRequest request, Exception? exception = null)
    {
        var state = exception == null ? $"Completed" : "Failed";
        var direction = request.Type.IsUpload() ? "Upload" : "Download";
        var msg = $"{direction} of {Path.GetFileName(request.LocalFilePath)} {state}";

        await conn.Log("HTTP Transfer", msg, exception?.ToString());
        await notificationManager.Send("HTTP Transfer", msg);
    }
}

#if ANDROID

public partial class MyHttpTransferDelegate : IAndroidForegroundServiceDelegate
{
    public void Configure(AndroidX.Core.App.NotificationCompat.Builder builder)
    {
        builder
            .SetContentTitle("Shiny Sample")
            .SetContentText("Running HTTP Transfers in the background");
    }
}

#endif