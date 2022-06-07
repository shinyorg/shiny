using System.Threading.Tasks;

namespace Shiny.Notifications;


#if ANDROID
public interface INotificationCustomizer
{
    //public static string? DefaultSmallIconResourceName { get; set; }
    //public static string? DefaultLargeIconResourceName { get; set; }
    //public static string? DefaultColorResourceName { get; set; }
    //public static bool DefaultUseBigTextStyle { get; set; }
    //public static bool? DefaultShowWhen { get; set; }
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
    //public string? SmallIconResourceName { get; set; } = DefaultSmallIconResourceName;
    //public string? LargeIconResourceName { get; set; } = DefaultLargeIconResourceName;
    //public string? ColorResourceName { get; set; } = DefaultColorResourceName;
    //public bool OnGoing { get; set; }
    //public bool UseBigTextStyle { get; set; } = DefaultUseBigTextStyle;
    //public bool AutoCancel { get; set; } = true;
    //public bool? ShowWhen { get; set; } = DefaultShowWhen;
    //public DateTimeOffset? When { get; set; }
    Task Customize(Notification notification, Channel channel, Android.App.Notification nativeNotification);
}
#elif IOS
public interface INotificationCustomizer
{
    Task Customize(Notification notification, Channel channel, UserNotifications.UNNotification nativeNotification);
}

#endif