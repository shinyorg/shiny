using System;
using System.Linq;
using Microsoft.Extensions.Hosting;
using Foundation;


namespace Shiny.Hosting
{
    public static partial class Extensions
    {
        public static void UseShinyIos(this IHostBuilder builder)
        {
            var contentRoot = NSSearchPath
                .GetDirectories(NSSearchPathDirectory.LibraryDirectory, NSSearchPathDomain.User)
                .First();

            builder.UseContentRoot(contentRoot);
        }
    }
}