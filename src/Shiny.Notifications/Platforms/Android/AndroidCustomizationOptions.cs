using System;
using Android.Content;

namespace Shiny.Notifications;


public class AndroidCustomizationOptions
{
    public string? SmallIconResourceName { get; set; }
    public string? LargeIconResourceName { get; set; }
    public string? ColorResourceName { get; set; }
    public bool UseBigTextStyle { get; set; }
    public Type? LaunchActivityType { get; set; }
    public ActivityFlags LaunchActivityFlags { get; set; } = ActivityFlags.NewTask | ActivityFlags.ClearTask;
    public string? Ticker { get; set; }
    public bool OnGoing { get; set; }
    public bool AutoCancel { get; set; } = true;
}
