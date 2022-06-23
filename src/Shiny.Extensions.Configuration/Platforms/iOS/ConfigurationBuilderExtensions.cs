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
        public static IConfigurationBuilder AddJsonIosBundle(this IConfigurationBuilder builder, string fileName = "appsettings.json", bool optional = true, bool reloadOnChange = true)
        {
#if MACCATALYST
            var path = Path.Combine(NSBundle.MainBundle.BundlePath, "Contents", "Resources", fileName);
#else
            var path = Path.Combine(NSBundle.MainBundle.BundlePath, fileName);
#endif
            return builder.AddJsonFile(path, optional, reloadOnChange);
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