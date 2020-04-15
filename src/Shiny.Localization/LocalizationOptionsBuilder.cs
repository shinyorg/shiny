using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Shiny.Localization
{
    public class LocalizationOptionsBuilder
    {
        readonly LocalizationOptions localizationOptions;

        public LocalizationOptionsBuilder(LocalizationOptions localizationOptions)
        {
            this.localizationOptions = localizationOptions;
        }

        internal LocalizationOptions LocalizationOptions => this.localizationOptions;

        /// <summary>
        /// True to initialize localization on app startup from a background job (default: true)
        /// False to initialize localization on demand
        /// </summary>
        /// <param name="autoInitialize"></param>
        /// <returns></returns>
        public virtual LocalizationOptionsBuilder EnableAutoInitialization(bool autoInitialize)
        {
            this.localizationOptions.AutoInitialize = autoInitialize;
            return this;
        }

        public virtual LocalizationOptionsBuilder UseInitializationCulture(CultureInfo initializationCulture)
        {
            this.localizationOptions.InitializationCulture = initializationCulture;
            return this;
        }

        public virtual LocalizationOptionsBuilder AddTextProvider<TExtraTextProvider>()
            where TExtraTextProvider : class, ITextProvider
        {
            this.LocalizationOptions.ExtraTextProviders.Add(typeof(TExtraTextProvider));
            return this;
        }
    }
}
