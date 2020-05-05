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
        /// Adjust auto initialization settings
        /// </summary>
        /// <param name="autoInitialize">True to initialize localization on app startup from a background job or False for on demand initialization (default: true)</param>
        /// <param name="tryParents">Try with parent culture up to invariant when the asked one can't be found (default: true)</param>
        /// <param name="refreshAvailableCultures">Refresh AvailableCultures property during initialization (default: true)</param>
        /// <param name="initializationCulture">Culture used for auto initialization</param>
        /// <returns></returns>
        public virtual LocalizationOptionsBuilder WithAutoInitialization(bool autoInitialize = true, bool tryParents = true, bool refreshAvailableCultures = true, CultureInfo? initializationCulture = null)
        {
            this.localizationOptions.AutoInitialize = autoInitialize;
            if (autoInitialize)
            {
                this.localizationOptions.TryParents = tryParents;
                this.localizationOptions.RefreshAvailableCultures = refreshAvailableCultures;
                this.localizationOptions.InitializationCulture = initializationCulture;
            }
            return this;
        }

        /// <summary>
        /// Specify the default culture used as invariant for all text providers
        /// </summary>
        /// <param name="defaultInvariantCulture">Culture used as invariant (default: null = InvariantCulture)</param>
        /// <returns></returns>
        public virtual LocalizationOptionsBuilder WithDefaultInvariantCulture(CultureInfo defaultInvariantCulture)
        {
            this.localizationOptions.DefaultInvariantCulture = defaultInvariantCulture;
            return this;
        }

        /// <summary>
        /// Add some extra text providers
        /// </summary>
        /// <typeparam name="TExtraTextProvider">Type of text provider</typeparam>
        /// <param name="invariantCulture">Culture used as invariant for this text provider (default: null = InvariantCulture)</param>
        /// <returns></returns>
        public virtual LocalizationOptionsBuilder AddTextProvider<TExtraTextProvider>(CultureInfo? invariantCulture = null)
            where TExtraTextProvider : class, ITextProvider =>
            this.AddTextProvider(typeof(TExtraTextProvider), invariantCulture);

        /// <summary>
        /// Add some extra text providers
        /// </summary>
        /// <param name="extraTextProviderType">Type of text provider</param>
        /// <param name="invariantCulture">Culture used as invariant for this text provider (default: null = InvariantCulture)</param>
        /// <returns></returns>
        public virtual LocalizationOptionsBuilder AddTextProvider(Type extraTextProviderType, CultureInfo? invariantCulture = null)
        {
            this.LocalizationOptions.TextProviders.Add(extraTextProviderType, invariantCulture);
            return this;
        }
    }
}
