using Shiny.Net.Http;
using Shiny.Notifications;

namespace Sample.HttpTransfers;


public partial class MyHttpTransferDelegate : IHttpTransferDelegate
{
    readonly INotificationManager notificationManager;
    readonly SampleSqliteConnection conn;


    public MyHttpTransferDelegate(INotificationManager notificationManager, SampleSqliteConnection conn)
    {
        this.notificationManager = notificationManager;
        this.conn = conn;
    }


    public Task OnError(HttpTransferRequest request, Exception ex)
        => this.CreateHttpTransferEvent(request, ex);


    public Task OnCompleted(HttpTransferRequest request)
        => this.CreateHttpTransferEvent(request);


    async Task CreateHttpTransferEvent(HttpTransferRequest request, Exception? exception = null)
    {
        var state = exception == null ? $"Completed" : "Failed";
        var direction = request.IsUpload ? "Upload" : "Download";
        var msg = $"{direction} of {Path.GetFileName(request.LocalFilePath)} {state}";

        await this.conn.Log("HTTP Transfer", msg, exception?.ToString());
        await this.notificationManager.Send("HTTP Transfer", msg);
    }
}

#if ANDROID
public partial class MyHttpTransferDelegate : IAndroidHttpTransferDelegate
{
    public void ConfigureNotification(AndroidX.Core.App.NotificationCompat.Builder builder, HttpTransferResult transfer)
    {
        switch (transfer.Status)
        {
            case HttpTransferState.Pending:
                if (transfer.Request.IsUpload)
                {
                    builder.SetContentText($"Starting Upload {Path.GetFileName(transfer.Request.LocalFilePath)} to {transfer.Request.Uri}");
                }
                else
                {
                    builder.SetContentText($"Start Download from {transfer.Request.Uri}");
                }
                break;

            case HttpTransferState.Paused:
            case HttpTransferState.PausedByNoNetwork:
            case HttpTransferState.PausedByCostedNetwork:
                var type = transfer.Request.IsUpload ? "Upload" : "Download";
                builder.SetContentText($"Paused {type} for {transfer.Request.Uri}");
                break;

            case HttpTransferState.InProgress:
                if (transfer.Request.IsUpload)
                {
                    builder.SetContentText($"Uploading {Path.GetFileName(transfer.Request.LocalFilePath)}");
                }
                else
                {
                    builder.SetContentText($"Downloading file from {transfer.Request.Uri}");
                }
                break;
        }
    }
}
#endif