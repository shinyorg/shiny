using System.Globalization;
using Microsoft.Extensions.DependencyInjection;

namespace Shiny.Localization
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Use localization with the default LocalizationManager
        /// </summary>
        /// <param name="services"></param>
        /// <param name="autoInitialize">Initialize localization on startup in a background job</param>
        /// <param name="defaultCulture">CurrentUICulture or InvariantCulture if defaultCulture is not specified here</param>
        /// <returns></returns>
        public static LocalizationBuilder<LocalizationManager> UseLocalization(this IServiceCollection services, bool autoInitialize = true, CultureInfo? defaultCulture = null) =>
            new LocalizationBuilder<LocalizationManager>(services, autoInitialize, defaultCulture);

        /// <summary>
        /// Use localization with custom delegate
        /// </summary>
        /// <typeparam name="TLocalizationManager">Your custom localization manager</typeparam>
        /// <param name="services"></param>
        /// <param name="autoInitialize">Initialize localization on startup in a background job</param>
        /// <param name="defaultCulture">CurrentUICulture or InvariantCulture if defaultCulture is not specified here</param>
        /// <returns></returns>
        public static LocalizationBuilder<TLocalizationManager> UseLocalization<TLocalizationManager>(this IServiceCollection services, bool autoInitialize = true, CultureInfo? defaultCulture = null)
            where TLocalizationManager : class, ILocalizationManager =>
            new LocalizationBuilder<TLocalizationManager>(services, autoInitialize, defaultCulture);
    }
}
