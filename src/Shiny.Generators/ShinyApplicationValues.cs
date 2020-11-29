using System;
using System.Linq;

using Microsoft.CodeAnalysis;


namespace Shiny.Generators
{
    internal class ShinyApplicationValues
    {
        public ShinyApplicationValues(AttributeData data)
        {
            this.ShinyStartupTypeName = To<string>(data, nameof(ShinyStartupTypeName), null);
            this.XamarinFormsAppTypeName = To<string>(data, nameof(XamarinFormsAppTypeName), null);
            this.ExcludeShinyUserModules = To(data, nameof(ExcludeShinyUserModules), false);
            this.ExcludeShinyUserJobs = To(data, nameof(ExcludeShinyUserJobs), false);
        }

        public string? ShinyStartupTypeName { get; set; }
        public string? XamarinFormsAppTypeName { get; set; }
        public bool ExcludeShinyUserModules { get; set; }
        public bool ExcludeShinyUserJobs { get; set; }

        static T To<T>(AttributeData data, string key, T defaultValue)
            => (T)data
                .NamedArguments
                .Where(x => x.Key == key)
                .Select(x => x.Value)
                .FirstOrDefault().Value ?? defaultValue;
    }
}
