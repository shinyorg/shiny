//using Microsoft.Extensions.Logging;

//namespace Shiny.Net.Http;


//public class AndroidPerTransferTransferNotification : IShinyStartupTask
//{
//    public AndroidPerTransferTransferNotification(
//        AndroidPlatform platform,
//        IHttpTransferManager manager,
//        ILogger<AndroidPerTransferTransferNotification> logger
//    )
//    {
//    }

//    public void Start()
//    {

//    }
//}

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