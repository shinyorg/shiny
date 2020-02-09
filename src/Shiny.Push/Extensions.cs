using System;
using Shiny.Settings;


namespace Shiny.Push
{
    static class Extensions
    {
        public static void SetRegToken(this ISettings settings, string? regToken)
            => SetOrRemove(settings, nameof(IPushManager.CurrentRegistrationToken), regToken);

        public static void SetRegDate(this ISettings settings, DateTime? dateTime)
            => SetOrRemove(settings, nameof(IPushManager.CurrentRegistrationTokenDate), dateTime);

        static void SetOrRemove<T>(ISettings settings, string key, T value)
        {
            if (value == null)
                settings.Remove(key);
            else
                settings.Set(key, value);
        }
    }
}
