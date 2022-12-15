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


    public Task OnError(IHttpTransfer transfer, Exception ex)
        => this.CreateHttpTransferEvent(transfer, ex);


    public Task OnCompleted(IHttpTransfer transfer)
        => this.CreateHttpTransferEvent(transfer);


    async Task CreateHttpTransferEvent(IHttpTransfer transfer, Exception? exception = null)
    {
        var state = transfer.Status == HttpTransferState.Completed ? $"Completed" : "Failed";
        var direction = transfer.Request.IsUpload ? "Upload" : "Download";
        var msg = $"{direction} of {Path.GetFileName(transfer.Request.LocalFilePath)} {state}";

        await this.conn.Log("HTTP Transfer", msg, exception?.ToString());
        await this.notificationManager.Send("HTTP Transfer", msg);
    }
}

#if ANDROID
public partial class MyHttpTransferDelegate : IAndroidForegroundServiceDelegate
{
    public void Configure(AndroidX.Core.App.NotificationCompat.Builder builder)
    {
        builder.SetContentText("Overriding HTTP Tranfer text from Sample");
    }
}
#endif