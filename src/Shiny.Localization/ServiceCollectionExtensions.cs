using System;
using System.Globalization;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Jobs;

namespace Shiny.Localization
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
            UseLocalization<TTextProvider, LocalizationManager>(services, optionsAction);

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
            where TLocalizationManager : class, ILocalizationManager
        {
            // Add default text provider
            services.AddSingleton<ITextProvider, TTextProvider>();

            // Add extra text providers
            var options = CreateLocalizationOptions(optionsAction);
            foreach (var extraTextProvider in options.ExtraTextProviders)
            {
                services.AddSingleton(typeof(ITextProvider), extraTextProvider);
            }

            // Add localization manager
            services.AddSingleton<ILocalizationManager, TLocalizationManager>();

            // Add initialization job if needed
            if (options.AutoInitialize)
            {
                var initializationJob = new JobInfo(typeof(LocalizationJob))
                {
                    Repeat = true,
                    IsSystemJob = true
                };

                // Add initialization culture if specified
                if (options.InitializationCulture != null)
                    initializationJob.Parameters.Add(nameof(options.InitializationCulture), options.InitializationCulture);

                services.RegisterJob(initializationJob);
            }

            return true;
        }

        static LocalizationOptions CreateLocalizationOptions(Action<LocalizationOptionsBuilder>? optionsAction = null)
        {
            var builder = new LocalizationOptionsBuilder(new LocalizationOptions());

            optionsAction?.Invoke(builder);

            return builder.LocalizationOptions;
        }
    }
}
