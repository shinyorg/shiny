using System;
using System.Threading.Tasks;
using Native = Android.App.Notification;

namespace Shiny.Notifications;



public class DefaultAndroidNotificationCustomizer : INotificationCustomizer
{
    readonly AndroidCustomizationOptions? options;
    readonly Action<Notification, Channel, Native>? onCustomize;

    public DefaultAndroidNotificationCustomizer(AndroidCustomizationOptions? options, Action<Notification, Channel, Native>? onCustomize)
    {
        this.options = options;
        this.onCustomize = onCustomize;
    }


    public Task Customize(Notification notification, Channel channel, Android.App.Notification nativeNotification)
    {
        //.SetSmallIcon(this.platform.GetSmallIconResource(notification.Android.SmallIconResourceName))
        //.SetAutoCancel(notification.Android.AutoCancel)
        //.SetOngoing(notification.Android.OnGoing);

        //await this.Services.Platform.TrySetImage(notification.ImageUri, builder);

        //if (!notification.Thread.IsEmpty())
        //    builder.SetGroup(notification.Thread);

        //this.ApplyLaunchIntent(builder, notification);
        //if (!notification.Android.ContentInfo.IsEmpty())
        //    builder.SetContentInfo(notification.Android.ContentInfo);

        //if (!notification.Android.Ticker.IsEmpty())
        //    builder.SetTicker(notification.Android.Ticker);

        //if (notification.Android.UseBigTextStyle)
        //    builder.SetStyle(new NotificationCompat.BigTextStyle().BigText(notification.Message));
        //else
        //    builder.SetContentText(notification.Message);

        //this.platform.TrySetLargeIconResource(notification.Android.LargeIconResourceName, builder);

        //if (!notification.Android.ColorResourceName.IsEmpty())
        //{
        //    var color = this.platform.GetColorResourceId(notification.Android.ColorResourceName!);
        //    builder.SetColor(color);
        //}

        //if (notification.Android.ShowWhen != null)
        //    builder.SetShowWhen(notification.Android.ShowWhen.Value);

        //if (notification.Android.When != null)
        //    builder.SetWhen(notification.Android.When.Value.ToUnixTimeMilliseconds());
        return Task.CompletedTask;
    }
}


public class AndroidCustomizationOptions
{
    //public string? SmallIconResourceName { get; set; }
    //public string? LargeIconResourceName { get; set; }
    //public string? ColorResourceName { get; set; }
    //public bool UseBigTextStyle { get; set; }
    //public bool? ShowWhen { get; set; }
    ////public static bool DefaultVibrate { get; set; }
    //public static Type? DefaultLaunchActivityType { get; set; }
    //public static ActivityFlags DefaultLaunchActivityFlags { get; set; } = ActivityFlags.NewTask | ActivityFlags.ClearTask;

    //public Type? LaunchActivityType { get; set; }
    //public ActivityFlags LaunchActivityFlags { get; set; } = DefaultLaunchActivityFlags;
    ////public bool Vibrate { get; set; } = DefaultVibrate;
    ////public int? Priority { get; set; }
    //public string? ContentInfo { get; set; }
    //public string? Ticker { get; set; }
    //public string? Category { get; set; }
    //public bool OnGoing { get; set; }
    //public bool UseBigTextStyle { get; set; } = DefaultUseBigTextStyle;
    //public bool AutoCancel { get; set; } = true;
}