using System;
using Android.Content;
using AndroidX.Core.App;

namespace Shiny.Notifications;


public class AndroidNotification : Notification
{
    public bool AutoCancel { get; set; } = true;
    public bool OnGoing { get; set; }
    public string? Ticket { get; set; }

    public string? Category { get; set; }
    public string? SmallIconResourceName { get; set; }
    public string? LargeIconResourceName { get; set; }
    public string? ColorResourceName { get; set; }
    public bool UseBigTextStyle { get; set; }
    public Type? LaunchActivityType { get; set; }
    public ActivityFlags LaunchActivityFlags { get; set; } = ActivityFlags.NewTask | ActivityFlags.ClearTask;
    public string? Ticker { get; set; }
}