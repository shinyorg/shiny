using System;
using Shiny.Infrastructure;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny.Jobs
{
    public static class Extensions
    {
        public static void SetValue<T>(this JobInfo job, string key, T value)
            => job.Parameters[key] = value;


        public static T GetValue<T>(this JobInfo job, string key, T defaultValue = default)
        {
            if (!job.Parameters.ContainsKey(key))
                return defaultValue;

            var value = job.Parameters[key];
            if (typeof(T).IsPrimitive)
                return (T)Convert.ChangeType(value, typeof(T));

            if (value is string s && typeof(T) != typeof(string))
                return ShinyHost.Container.GetService<ISerializer>().Deserialize<T>(s);

            // TODO: Jobject & jarray tend to emerge causing this to fail
            return defaultValue;
        }
    }
}
