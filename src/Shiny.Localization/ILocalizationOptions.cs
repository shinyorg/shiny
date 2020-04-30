using System;
using System.Collections.Generic;
using System.Globalization;

namespace Shiny.Localization
{
    public interface ILocalizationOptions
    {
        IDictionary<Type, CultureInfo?> TextProviders { get; }
        Type LocalizationManagerType { get; }
        bool AutoInitialize { get; }
        bool TryParents { get; } 
        bool RefreshAvailableCultures { get; }
        CultureInfo? InitializationCulture { get; }
        CultureInfo? DefaultInvariantCulture { get; }
    }
}