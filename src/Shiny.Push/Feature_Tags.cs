using System.Threading.Tasks;


namespace Shiny.Push
{
    public static class FeatureTags
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
            if (result.Status == AccessState.Available)
                await pushManager.TrySetTags(tags);

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


        /// <summary>
        /// If the push manager supports tags, this will call the AddTag method
        /// </summary>
        /// <param name="pushManager"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static Task TryAddTag(this IPushManager pushManager, string tag)
            => (pushManager as IPushTagSupport)?.AddTag(tag) ?? Task.CompletedTask;


        /// <summary>
        /// If the push manager supports tags, this will call the RemoveTag method
        /// </summary>
        /// <param name="pushManager"></param>
        /// <param name="tag"></param>
        /// <returns></returns>
        public static Task TryRemoveTag(this IPushManager pushManager, string tag)
            => (pushManager as IPushTagSupport)?.RemoveTag(tag) ?? Task.CompletedTask;


        /// <summary>
        /// If the push manager supports tags, this will call the ClearTags method
        /// </summary>
        /// <param name="pushManager"></param>
        /// <returns></returns>
        public static Task TryClearTags(this IPushManager pushManager)
            => (pushManager as IPushTagSupport)?.ClearTags() ?? Task.CompletedTask;
    }
}
