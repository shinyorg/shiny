using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;


namespace Shiny
{
    public static class PlatformExtensions
    {
        //public static void ConfigureJobs(this IServiceCollection services)
        //{
        //    //JobBackgroundTask.PeriodicRunTime
        //}


        public static void Dispatch(this Action action)
        {
            var dispatcher = CoreApplication.MainView.CoreWindow?.Dispatcher;

            if (dispatcher == null)
                throw new NullReferenceException("Main thread missing");

            if (dispatcher.HasThreadAccess)
                action();
            else
                dispatcher.RunAsync(CoreDispatcherPriority.Normal, () => action());
        }
    }
}
