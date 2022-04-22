using System;
using Android.App;
using Android.Content;
using Shiny.Infrastructure;
using Shiny.Notifications;


namespace Shiny
{
    static class PlatformExtensions
    {
        internal static ActivityFlags ToNative(this AndroidActivityFlags flags)
        {
            var intValue = (int) flags;
            var native = (ActivityFlags) intValue;
            return native;
        }


        const string AndroidBadgeCountKey = "AndroidBadge";
        internal static int GetBadgeCount(this ShinyCoreServices core)
            => core.Settings.Get(AndroidBadgeCountKey, 0);


        internal static void SetBadgeCount(this ShinyCoreServices core, int value)
        {
            core.Settings.Set(AndroidBadgeCountKey, value);

            if (value <= 0)
                global::XamarinShortcutBadger.ShortcutBadger.RemoveCount(core.Platform.AppContext);
            else
                global::XamarinShortcutBadger.ShortcutBadger.ApplyCount(core.Platform.AppContext, value);

        }

        internal static NotificationImportance ToNative(this ChannelImportance importance) => importance switch
        {
            ChannelImportance.Critical => NotificationImportance.Max,
            ChannelImportance.High => NotificationImportance.High,
            ChannelImportance.Normal => NotificationImportance.Default,
            ChannelImportance.Low => NotificationImportance.Low,
            _ => throw new InvalidOperationException("Invalid value - " + importance)
        };
    }
}