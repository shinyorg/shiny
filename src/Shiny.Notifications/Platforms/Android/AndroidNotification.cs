using System;
using System.Threading.Tasks;
using Android.App;
using Android.Content;
using AndroidX.Core.App;

namespace Shiny.Notifications;


public class AndroidNotification : Notification
{
    public bool AutoCancel { get; set; } = true;
    public bool OnGoing { get; set; }
    public string? Ticket { get; set; }

    // changing this will break
    public PendingIntent? LaunchIntent { get; set; }

    public string? SmallIconResourceName { get; set; }
    public string? LargeIconResourceName { get; set; }
    public string? ColorResourceName { get; set; }
    public bool UseBigTextStyle { get; set; }
    public Type? LaunchActivityType { get; set; }
    public ActivityFlags LaunchActivityFlags { get; set; } = ActivityFlags.NewTask | ActivityFlags.ClearTask;
    public string? Ticker { get; set; }
}