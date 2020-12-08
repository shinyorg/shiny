using System;
using System.Linq;
using Microsoft.CodeAnalysis;


namespace Shiny.Generators
{
    public class ShinyApplicationValues
    {
        public ShinyApplicationValues() { }
        public ShinyApplicationValues(AttributeData data)
        {
            this.ShinyStartupTypeName = To<string>(data, nameof(ShinyStartupTypeName), null);
            this.XamarinFormsAppTypeName = To<string>(data, nameof(XamarinFormsAppTypeName), null);
            this.ExcludeJobs = To(data, nameof(ExcludeJobs), false);
            this.ExcludeModules = To(data, nameof(ExcludeModules), false);
            this.ExcludeStartupTasks = To(data, nameof(ExcludeStartupTasks), false);
            this.ExcludeServices = To(data, nameof(ExcludeServices), false);
        }

        public string? ShinyStartupTypeName { get; set; }
        public string? XamarinFormsAppTypeName { get; set; }
        public bool ExcludeModules { get; set; }
        public bool ExcludeJobs { get; set; }
        public bool ExcludeStartupTasks { get; set; }
        public bool ExcludeServices { get; set; }
        //public bool ExcludeXamarinEssentialsRegistration

        static T To<T>(AttributeData data, string key, T defaultValue)
        {
            var query = data
                .NamedArguments
                .Where(x => x.Key == key);

            if (!query.Any())
                return defaultValue;

            return (T)query.First().Value.Value ?? defaultValue;
        }
    }
}
