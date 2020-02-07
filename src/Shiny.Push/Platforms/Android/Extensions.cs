using System;
using Shiny.Settings;


namespace Shiny.Push
{
    static class Extensions
    {
        public static void SetRegToken(this ISettings settings, string regToken)
            => settings.Set(nameof(IPushManager.CurrentRegistrationToken), regToken);

        public static void SetRegDate(this ISettings settings, DateTime? dateTime)
            => settings.Set(nameof(IPushManager.CurrentRegistrationTokenDate), dateTime);
    }
}
