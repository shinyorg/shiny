using System;
using System.Threading;
using System.Threading.Tasks;
using Shiny.Settings;


namespace Shiny.Push
{
    public static class Extensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="push"></param>
        /// <returns></returns>
        public static bool IsTagsSupport(this IPushManager push)
            => push is IPushTagSupport;


        /// <summary>
        ///
        /// </summary>
        /// <param name="push"></param>
        /// <returns></returns>
        public static string[]? TryGetTags(this IPushManager push)
            => (push as IPushTagSupport)?.RegisteredTags;


        /// <summary>
        ///
        /// </summary>
        /// <param name="pushManager"></param>
        /// <param name="tags"></param>
        /// <param name="cancelToken"></param>
        /// <returns></returns>
        public static async Task<PushAccessState> TryRequestAccessWithTags(this IPushManager pushManager, string[] tags, bool throwOnFail = false, CancellationToken cancelToken = default)
        {
            var result = await pushManager.RequestAccess(cancelToken);
            if (pushManager is IPushTagSupport tagEnabled)
                await tagEnabled.SetTags(tags);

            return result;
        }


        public static async Task<bool> TrySetTags(this IPushManager pushManager, params string[] tags)
        {
            if (pushManager is IPushTagSupport tagEnabled)
            { 
                await tagEnabled.SetTags(tags);
                return true;
            }
            return false;
        }


        internal static void SetRegToken(this ISettings settings, string? regToken)
            => SetOrRemove(settings, nameof(IPushManager.CurrentRegistrationToken), regToken);

        internal static void SetRegDate(this ISettings settings, DateTime? dateTime)
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
