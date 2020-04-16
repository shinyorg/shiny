using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Jobs;

namespace Shiny.Localization
{
    public class LocalizationModule : ShinyModule
    {
        readonly Type textProviderType;
        readonly Type localizationManagerType;
        readonly LocalizationOptions options;

        public LocalizationModule(Type textProviderType, Type localizationManagerType, Action<LocalizationOptionsBuilder>? optionsAction = null)
        {
            if(!typeof(ITextProvider).IsAssignableFrom(textProviderType))
                throw new ArgumentException($"Your text provider class must inherit from ITextProvider interface or derived");

            if (!typeof(ILocalizationManager).IsAssignableFrom(localizationManagerType))
                throw new ArgumentException($"Your localization manager class must inherit from ILocalizationManager interface or derived");

            this.textProviderType = textProviderType;
            this.localizationManagerType = localizationManagerType;
            this.options = this.CreateLocalizationOptions(optionsAction);
        }

        public override void Register(IServiceCollection services)
        {
            // Add default text provider
            services.AddSingleton(typeof(ITextProvider), this.textProviderType);

            // Add extra text providers
            foreach (var extraTextProvider in this.options.ExtraTextProviders)
            {
                services.AddSingleton(typeof(ITextProvider), extraTextProvider);
            }

            // Add localization manager
            services.AddSingleton(typeof(ITextProvider), this.localizationManagerType);

            // Add initialization job if needed
            if (this.options.AutoInitialize)
            {
                var initializationJob = new JobInfo(typeof(LocalizationJob))
                {
                    Repeat = true,
                    IsSystemJob = true
                };

                // Add initialization culture if specified
                if (this.options.InitializationCulture != null)
                    initializationJob.Parameters.Add(nameof(this.options.InitializationCulture), this.options.InitializationCulture);

                services.RegisterJob(initializationJob);
            }
        }

        LocalizationOptions CreateLocalizationOptions(Action<LocalizationOptionsBuilder>? optionsAction = null)
        {
            var builder = new LocalizationOptionsBuilder(new LocalizationOptions());

            optionsAction?.Invoke(builder);

            return builder.LocalizationOptions;
        }
    }
}
