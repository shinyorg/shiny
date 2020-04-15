using System;
using System.Collections.Generic;
using System.Globalization;
using System.Text;

namespace Shiny.Localization
{
    public class LocalizationOptions
    {
        public bool AutoInitialize { get; set; } = true;

        public CultureInfo? InitializationCulture { get; set; }

        public IList<Type> ExtraTextProviders { get; set; } = new List<Type>();
    }
}
