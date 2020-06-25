using System;
using Microsoft.Extensions.Hosting;


namespace Shiny.Hosting
{
    public static partial class Extensions
    {
        public static void UseShiny(this IHostBuilder builder) 
        {
            // TODO: replace IShinyStartupTask with IHostedService?  Async startup could be a bad idea
#if __IOS__
            builder.UseShinyIos();
#elif __ANDROID__
            builder.UseShinyAndroid();
#elif WINDOWS_UWP
            builder.UseShinyUwp();
#endif
        }
    }
}
