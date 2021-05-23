using System;


namespace Shiny
{
    public static class PlatformExtensions
    {
        public static void ShinyInit(this Windows.UI.Xaml.Application app, IShinyStartup? startup = null)
            => ShinyHost.Init(new UwpPlatform(app), startup);
    }
}
