using System.Threading.Tasks;
using UIKit;


namespace Shiny.Jobs
{
    public static class Extensions
    {


        /// <summary>
        /// Gets the background refresh status
        /// </summary>
        /// <param name="platform"></param>
        /// <returns></returns>
        public static Task<AccessState> GetBackgroundRefreshStatus(this IPlatform platform) => platform.InvokeOnMainThreadAsync(() =>
            UIApplication.SharedApplication.BackgroundRefreshStatus switch
            {
                UIBackgroundRefreshStatus.Available => AccessState.Available,
                UIBackgroundRefreshStatus.Denied => AccessState.Denied,
                UIBackgroundRefreshStatus.Restricted => AccessState.Restricted,
                _ => AccessState.Unknown
            }
        );
    }
}
