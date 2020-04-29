using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Shiny.Localization
{
    public class LocalizationOptions : ILocalizationOptions
    {
        public LocalizationOptions(Type textProviderType, Type localizationManagerType)
        {
            this.TextProviders = new Dictionary<Type, CultureInfo?>{ { textProviderType, null } };
            this.LocalizationManagerType = localizationManagerType;
        }

        public IDictionary<Type, CultureInfo?> TextProviders { get; }

        public Type LocalizationManagerType { get; internal set; }

        public bool AutoInitialize { get; internal set; } = true;

        public CultureInfo? InitializationCulture { get; internal set; }
    }
}
