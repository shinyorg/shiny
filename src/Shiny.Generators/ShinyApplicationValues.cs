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
            this.ExcludeJobs = To(data, nameof(ExcludeJobs), false);
            this.ExcludeModules = To(data, nameof(ExcludeModules), false);
            this.ExcludeStartupTasks = To(data, nameof(ExcludeStartupTasks), false);
        }

        public string? ShinyStartupTypeName { get; set; }
        public string? XamarinFormsAppTypeName { get; set; }
        public bool ExcludeModules { get; set; }
        public bool ExcludeJobs { get; set; }
        public bool ExcludeStartupTasks { get; set; }
        //public bool ExcludeXamarinEssentialsRegistration

        static T To<T>(AttributeData data, string key, T defaultValue)
            => (T)data
                .NamedArguments
                .Where(x => x.Key == key)
                .Select(x => x.Value)
                .FirstOrDefault().Value ?? defaultValue;
    }
}
