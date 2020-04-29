using System;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
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

        /// <summary>
        /// Specify the culture used as invariant for the main text provider
        /// </summary>
        /// <param name="invariantCulture">Culture used as invariant</param>
        /// <returns></returns>
        public virtual LocalizationOptionsBuilder UseMainTextProviderInvariantCulture(CultureInfo invariantCulture)
        {
            this.localizationOptions.TextProviders[this.localizationOptions.TextProviders.Keys.First()] = invariantCulture;
            return this;
        }

        /// <summary>
        /// Specify your default initialization culture
        /// </summary>
        /// <param name="initializationCulture"></param>
        /// <returns></returns>
        public virtual LocalizationOptionsBuilder UseInitializationCulture(CultureInfo initializationCulture)
        {
            this.localizationOptions.InitializationCulture = initializationCulture;
            return this;
        }

        /// <summary>
        /// Add some extra text providers
        /// </summary>
        /// <typeparam name="TExtraTextProvider">Type of text provider</typeparam>
        /// <param name="invariantCulture">Culture used as invariant</param>
        /// <returns></returns>
        public virtual LocalizationOptionsBuilder AddTextProvider<TExtraTextProvider>(CultureInfo? invariantCulture = null)
            where TExtraTextProvider : class, ITextProvider =>
            this.AddTextProvider(typeof(TExtraTextProvider), invariantCulture);

        /// <summary>
        /// Add some extra text providers
        /// </summary>
        /// <param name="extraTextProviderType">Type of text provider</param>
        /// <param name="invariantCulture">Culture used as invariant</param>
        /// <returns></returns>
        public virtual LocalizationOptionsBuilder AddTextProvider(Type extraTextProviderType, CultureInfo? invariantCulture = null)
        {
            this.LocalizationOptions.TextProviders.Add(extraTextProviderType, invariantCulture);
            return this;
        }
    }
}
