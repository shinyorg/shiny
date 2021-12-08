using System.Threading.Tasks;
using UIKit;


namespace Shiny.Jobs
{
    public static class Extensions
    {
        /// <summary>
        /// Sets the background fetch interval
        /// </summary>
        /// <param name="platform"></param>
        /// <param name="BackgroundFetchInterval">Set as null to use iOS minimum value</param>
        /// <returns></returns>
        public static Task SetBackgroundFetchInterval(this IPlatform platform, double? BackgroundFetchInterval) => platform.InvokeOnMainThreadAsync(() =>
        {
            var fetch = BackgroundFetchInterval ?? UIApplication.BackgroundFetchIntervalMinimum;
            JobManager.BackgroundFetchInterval = fetch;
            UIApplication.SharedApplication.SetMinimumBackgroundFetchInterval(fetch);
        });


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
