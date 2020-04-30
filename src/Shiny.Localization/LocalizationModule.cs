using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Jobs;

namespace Shiny.Localization
{
    public class LocalizationModule : ShinyModule
    {
        readonly LocalizationOptions options;

        public LocalizationModule(Type textProviderType, Type localizationManagerType, Action<LocalizationOptionsBuilder>? optionsAction = null)
        {
            if(!typeof(ITextProvider).IsAssignableFrom(textProviderType))
                throw new ArgumentException($"Your text provider class must inherit from ITextProvider interface or derived");

            if (!typeof(ILocalizationManager).IsAssignableFrom(localizationManagerType))
                throw new ArgumentException($"Your localization manager class must inherit from ILocalizationManager interface or derived");

            this.options = this.CreateLocalizationOptions(textProviderType, localizationManagerType, optionsAction);
        }

        public override void Register(IServiceCollection services)
        {
            // Add localization options
            services.AddSingleton<ILocalizationOptions>(this.options);

            // Add text providers
            foreach (var textProvider in this.options.TextProviders)
            {
                if (!typeof(ITextProvider).IsAssignableFrom(textProvider.Key))
                    throw new ArgumentException($"Your text provider class must inherit from ITextProvider interface or derived");

                services.AddSingleton(typeof(ITextProvider), textProvider.Key);
            }

            // Add localization manager
            services.AddSingleton(typeof(ILocalizationManager), this.options.LocalizationManagerType);

            // Add initialization job if needed
            if (this.options.AutoInitialize)
            {
                var initializationJob = new JobInfo(typeof(LocalizationJob))
                {
                    Repeat = true,
                    IsSystemJob = true
                };

                services.RegisterJob(initializationJob);
            }
        }

        LocalizationOptions CreateLocalizationOptions(Type textProviderType, Type localizationManagerType, Action<LocalizationOptionsBuilder>? optionsAction = null)
        {
            var builder = new LocalizationOptionsBuilder(new LocalizationOptions(textProviderType, localizationManagerType));

            optionsAction?.Invoke(builder);

            return builder.LocalizationOptions;
        }
    }
}
