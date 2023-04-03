using System;
using Android.App;
using Shiny.Notifications;

namespace Shiny;


static class PlatformExtensions
{
    internal static NotificationImportance ToNative(this ChannelImportance importance) => importance switch
    {
        ChannelImportance.Critical => NotificationImportance.Max,
        ChannelImportance.High => NotificationImportance.High,
        ChannelImportance.Normal => NotificationImportance.Default,
        ChannelImportance.Low => NotificationImportance.Low,
        _ => throw new InvalidOperationException("Invalid value - " + importance)
    };
}