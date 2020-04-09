using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Jobs;

namespace Shiny.Localization
{
    public class LocalizationBuilder<TLocalizationManager>
        where TLocalizationManager : class, ILocalizationManager
    {
        readonly IServiceCollection services;
        readonly CultureInfo? defaultCulture;

        internal LocalizationBuilder(IServiceCollection services, bool autoInitialize = true, CultureInfo? defaultCulture = null)
        {
            this.services = services;
            this.defaultCulture = defaultCulture;
            this.services.AddSingleton<ILocalizationManager, TLocalizationManager>();

            if (autoInitialize)
            {
                var initializationJob = new JobInfo(typeof(LocalizationJob))
                {
                    Repeat = true,
                    IsSystemJob = true
                };
                services.RegisterJob(initializationJob);
            }
        }

        /// <summary>
        /// When adding multiple text providers, default localization Delegate will search for first contained text key by provider addition order
        /// </summary>
        /// <typeparam name="TTextProvider">Could be Resx, Database, Json, Xml or whatever source implementing ITextProvider or derived interface</typeparam>
        /// <returns></returns>
        public LocalizationBuilder<TLocalizationManager> AddTextProvider<TTextProvider>() where TTextProvider : class, ITextProvider
        {
            this.services.AddSingleton<ITextProvider, TTextProvider>();

            return this;
        }
    }
}
