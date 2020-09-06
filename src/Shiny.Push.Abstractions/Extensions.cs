using System;
using System.Threading.Tasks;


namespace Shiny.Push
{
    public static class Extensions
    {
        /// <summary>
        /// Returns if tags are supported by this push manager
        /// </summary>
        /// <param name="push"></param>
        /// <returns></returns>
        public static bool IsTagsSupport(this IPushManager push)
            => push is IPushTagSupport;


        /// <summary>
        /// If manager supports tags, it will return them if set, otherwise it will return null
        /// </summary>
        /// <param name="push"></param>
        /// <returns></returns>
        public static string[]? TryGetTags(this IPushManager push)
            => (push as IPushTagSupport)?.RegisteredTags;


        /// <summary>
        /// If manager supports tags & permission is granted to push, it will set the tags
        /// </summary>
        /// <param name="pushManager"></param>
        /// <param name="tags"></param>
        /// <returns></returns>
        public static async Task<PushAccessState> TryRequestAccessWithTags(this IPushManager pushManager, params string[] tags)
        {
            var result = await pushManager.RequestAccess();
            if (pushManager is IPushTagSupport tagEnabled)
                await tagEnabled.SetTags(tags);

            return result;
        }


        /// <summary>
        /// If manager supports tags, it will set the tags
        /// </summary>
        /// <param name="pushManager"></param>
        /// <param name="tags"></param>
        /// <returns></returns>
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
