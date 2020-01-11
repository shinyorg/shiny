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


        internal static NotificationImportance ToNative(this AndroidNotificationImportance import)
        {
            var intValue = (int) import;
            var native = (NotificationImportance) intValue;
            return native;
        }


        internal static int GetColorByName(this AndroidContext context, string colorName) => context
            .AppContext
            .Resources
            .GetIdentifier(
                colorName,
                "color",
                context.AppContext.PackageName
            );

        internal static int GetResourceIdByName(this AndroidContext context, string iconName) => context
            .AppContext
            .Resources
            .GetIdentifier(
                iconName,
                "drawable",
                context.AppContext.PackageName
            );
    }
}