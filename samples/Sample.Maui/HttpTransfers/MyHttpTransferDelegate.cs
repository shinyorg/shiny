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


    public Task OnError(HttpTransfer transfer, Exception ex)
        => this.CreateHttpTransferEvent(transfer);


    public Task OnCompleted(HttpTransfer transfer)
        => this.CreateHttpTransferEvent(transfer);


    async Task CreateHttpTransferEvent(HttpTransfer transfer)
    {
        var state = transfer.Status == HttpTransferState.Completed ? $"Completed" : "Failed";
        var direction = transfer.IsUpload ? "Upload" : "Download";
        var msg = $"{direction} of {Path.GetFileName(transfer.LocalFilePath)} {state}";

        await this.conn.Log("HTTP Transfer", msg);
        await this.notificationManager.Send("HTTP Transfer", msg);
    }
}

#if ANDROID
public partial class MyHttpTransferDelegate : IAndroidForegroundServiceDelegate
{
    public void Configure(AndroidX.Core.App.NotificationCompat.Builder builder)
    {

    }
}
#endif