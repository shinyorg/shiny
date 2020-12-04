using System;
using Windows.ApplicationModel.Core;
using Windows.UI.Core;


namespace Shiny
{
    public static class PlatformExtensions
    {
        public static void ShinyInit(this Windows.UI.Xaml.Application app, IShinyStartup? startup = null)
            => ShinyHost.Init(new UwpPlatform(app), startup);


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
