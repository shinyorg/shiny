using System;
using Windows.ApplicationModel.Background;


namespace Shiny
{
    public static class PlatformExtensions
    {
        public static void ShinyInit<TBgTask>(this Windows.UI.Xaml.Application app, IShinyStartup startup) where TBgTask : IBackgroundTask
        {
            UwpPlatform.SetBackgroundTask(typeof(TBgTask));
            ShinyHost.Init(new UwpPlatform(app), startup);
        }


        public static void ShinyRunBackgroundTask(this IBackgroundTask task, IBackgroundTaskInstance taskInstance, IShinyStartup startup)
            => UwpPlatform.RunBackgroundTask(task, taskInstance, startup);
    }
}
