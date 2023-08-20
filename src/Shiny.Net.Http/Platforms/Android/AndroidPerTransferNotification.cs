using AndroidX.Core.App;
using System.Collections.Generic;
using Microsoft.Extensions.Logging;

namespace Shiny.Net.Http;


public class AndroidPerTransferTransferNotification : IShinyStartupTask
{
    readonly Dictionary<string, (int NotificationId, NotificationCompat.Builder Context)> dictionary = new();


    public AndroidPerTransferTransferNotification(
        AndroidPlatform platform,
        IHttpTransferManager manager,
        ILogger<AndroidPerTransferTransferNotification> logger
    )
    {
    }

    public void Start()
    {

    }
}

///*
//using System;
//using AndroidX.Core.App;

//namespace Shiny.Net.Http;


//public record HttpTransferProcessArgs(
//    int NotificationId,
//    NotificationCompat.Builder Builder,
//    NotificationManagerCompat Notifications,
//    Action OnComplete
//)
//{
//    public void SendNotification()
//    {
//        var not = this.Builder.Build();
//        this.Notifications.Notify(this.NotificationId, not);
//    }


//    public void CancelNotification()
//        => this.Notifications.Cancel(this.NotificationId);
//};


//void UpdateTransferNotification(HttpTransferProcessArgs args, HttpTransferResult transfer)
//    {
//        this.logger.LogDebug("Updating Foreground Notification");        
//        var percentComplete = transfer.IsDeterministic ? Convert.ToInt32(transfer.Progress.PercentComplete * 100) : 0;

//        args.Builder.SetContentText("Processing Background Transfers");
//        args.Builder.SetProgress(
//            100,
//            percentComplete,
//            transfer.IsDeterministic
//        );
//        this.delegates
//            .OfType<IAndroidHttpTransferDelegate>()
//            .ToList()
//            .ForEach(x =>
//            {
//                try
//                {
//                    x.ConfigureNotification(args.Builder, transfer);
//                }
//                catch (Exception ex)
//                {
//                    this.logger.LogWarning(ex, $"Error updating notification on user delegate: {x.GetType().FullName}");
//                }
//            });

//        args.SendNotification();
//        this.logger.LogDebug("Updated Foreground Notification");
//    } 
// */


//#if ANDROID
//public partial class MyHttpTransferDelegate : IAndroidHttpTransferDelegate
//{
//    public void ConfigureNotification(AndroidX.Core.App.NotificationCompat.Builder builder, HttpTransferResult transfer)
//    {
//        switch (transfer.Status)
//        {
//            case HttpTransferState.Pending:
//                if (transfer.Request.IsUpload)
//                {
//                    builder.SetContentText($"Starting Upload {Path.GetFileName(transfer.Request.LocalFilePath)} to {transfer.Request.Uri}");
//                }
//                else
//                {
//                    builder.SetContentText($"Start Download from {transfer.Request.Uri}");
//                }
//                break;

//            case HttpTransferState.Paused:
//            case HttpTransferState.PausedByNoNetwork:
//            case HttpTransferState.PausedByCostedNetwork:
//                var type = transfer.Request.IsUpload ? "Upload" : "Download";
//                builder.SetContentText($"Paused {type} for {transfer.Request.Uri}");
//                break;

//            case HttpTransferState.InProgress:
//                if (transfer.Request.IsUpload)
//                {
//                    builder.SetContentText($"Uploading {Path.GetFileName(transfer.Request.LocalFilePath)}");
//                }
//                else
//                {
//                    builder.SetContentText($"Downloading file from {transfer.Request.Uri}");
//                }
//                break;
//        }
//    }
//}
//#endif