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
        public static async Task<PushAccessState> TryRequestAccessWithTags(this IPushManager pushManager, string[] tags, CancellationToken cancelToken = default)
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
    }
}
