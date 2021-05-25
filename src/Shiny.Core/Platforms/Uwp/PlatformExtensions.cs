using System;


namespace Shiny
{
    public static class PlatformExtensions
    {
        public static void ShinyInit<TBgTask, TStartup>(this Windows.UI.Xaml.Application app)
                where TBgTask : ShinyBackgroundTask<TStartup>
                where TStartup : class, IShinyStartup, new()
            => ShinyHost.Init(new UwpPlatform(app), new TStartup());
    }
}
