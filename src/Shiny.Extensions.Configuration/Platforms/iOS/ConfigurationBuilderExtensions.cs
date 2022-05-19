using Foundation;
using Shiny.Extensions.Configuration;
using System.IO;


namespace Microsoft.Extensions.Configuration
{
    public static partial class ConfigurationBuilderExtensions
    {
        /// <summary>
        ///
        /// </summary>
        /// <param name="builder"></param>
        /// <param name="fileName"></param>
        /// <param name="optional"></param>
        /// <returns></returns>
        public static IConfigurationBuilder AddJsonIosBundle(this IConfigurationBuilder builder, string fileName = "appsettings.json", bool optional = true)
        {
            var path = Path.Combine(NSBundle.MainBundle.BundlePath, fileName);
            return builder.AddJsonFile(path, optional, false);
        }


        /// <summary>
        ///
        /// </summary>
        /// <param name="builder"></param>
        /// <returns></returns>
        public static IConfigurationBuilder AddIosUserDefaults(this IConfigurationBuilder builder)
            => builder.Add(new NSUserDefaultsConfigurationSource());
    }
}