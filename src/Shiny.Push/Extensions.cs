using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Settings;


namespace Shiny.Push
{
    static class Extensions
    {
        public static bool IsTagsSupport(this IPushManager push)
            => push is IPushTagEnabled;


        /// <summary>
        ///
        /// </summary>
        /// <param name="pushManager"></param>
        /// <param name="tags"></param>
        /// <param name="throwOnFail"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public static Task<PushAccessState> TryRequestAccessWithTags(this IPushManager pushManager, string[] tags, bool throwOnFail = false, CancellationToken cancelToken = default)
        {
            if (pushManager is IPushTagEnabled tagEnabled)
                return tagEnabled.RequestAccess(tags, cancelToken);

            if (throwOnFail)
                throw new ArgumentException("Push mananger is not tag enabled");

            return pushManager.RequestAccess(cancelToken);
        }


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
