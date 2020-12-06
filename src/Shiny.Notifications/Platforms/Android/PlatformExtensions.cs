using Android.App;
using Android.Content;
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


        internal static NotificationImportance ToNative(this ChannelImportance importance) => importance switch
        {
            ChannelImportance.Critical => NotificationImportance.Max,
            ChannelImportance.High => NotificationImportance.High,
            ChannelImportance.Normal => NotificationImportance.Default,
            ChannelImportance.Low => NotificationImportance.Low
        };


        internal static int GetColorByName(this IAndroidContext context, string colorName) => context
            .AppContext
            .Resources
            .GetIdentifier(
                colorName,
                "color",
                context.AppContext.PackageName
            );

        internal static int GetResourceIdByName(this IAndroidContext context, string iconName) => context
            .AppContext
            .Resources
            .GetIdentifier(
                iconName,
                "drawable",
                context.AppContext.PackageName
            );
    }
}