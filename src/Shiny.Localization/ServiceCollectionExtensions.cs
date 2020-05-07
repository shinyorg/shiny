using System;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Localization;

namespace Shiny
{
    public static class ServiceCollectionExtensions
    {
        /// <summary>
        /// Use localization plugin with a default text provider
        /// </summary>
        /// <typeparam name="TTextProvider">A default text provider</typeparam>
        /// <param name="services">The service collection</param>
        /// <param name="optionsAction">Some options to customize if needed</param>
        /// <returns></returns>
        public static bool UseLocalization<TTextProvider>(this IServiceCollection services,
            Action<LocalizationOptionsBuilder>? optionsAction = null)
            where TTextProvider : class, ITextProvider =>
            services.UseLocalization(typeof(TTextProvider), typeof(LocalizationManager), optionsAction);

        /// <summary>
        /// Use localization plugin with a default text provider and a custom localization manager
        /// </summary>
        /// <typeparam name="TTextProvider">A default text provider</typeparam>
        /// <typeparam name="TLocalizationManager">A custom localization manager</typeparam>
        /// <param name="services">The service collection</param>
        /// <param name="optionsAction">Some options to customize if needed</param>
        /// <returns></returns>
        public static bool UseLocalization<TTextProvider, TLocalizationManager>(this IServiceCollection services,
            Action<LocalizationOptionsBuilder>? optionsAction = null)
            where TTextProvider : class, ITextProvider
            where TLocalizationManager : class, ILocalizationManager =>
            services.UseLocalization(typeof(TTextProvider), typeof(TLocalizationManager), optionsAction);

        /// <summary>
        /// Use localization plugin with a default text provider and a custom localization manager
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="textProviderType">A default text provider</param>
        /// <param name="optionsAction">Some options to customize if needed</param>
        /// <returns></returns>
        public static bool UseLocalization(this IServiceCollection services, Type textProviderType,
            Action<LocalizationOptionsBuilder>? optionsAction = null)
        {
            services.RegisterModule(new LocalizationModule(textProviderType, typeof(LocalizationManager), optionsAction));

            return true;
        }

        /// <summary>
        /// Use localization plugin with a default text provider and a custom localization manager
        /// </summary>
        /// <param name="services">The service collection</param>
        /// <param name="textProviderType">A default text provider</param>
        /// <param name="localizationManagerType">A custom localization manager</param>
        /// <param name="optionsAction">Some options to customize if needed</param>
        /// <returns></returns>
        public static bool UseLocalization(this IServiceCollection services, Type textProviderType, Type localizationManagerType,
            Action<LocalizationOptionsBuilder>? optionsAction = null)
        {
            services.RegisterModule(new LocalizationModule(textProviderType, localizationManagerType, optionsAction));

            return true;
        }
    }
}
