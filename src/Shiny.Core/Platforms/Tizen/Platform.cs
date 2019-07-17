using System;
using Tizen.System;


namespace Shiny
{
    public enum PlatformNamespace
    {
        System,
        Feature
    }


    public static class Platform
    {
        public static T Get<T>(string item, PlatformNamespace ns = PlatformNamespace.Feature)
        {
            var uri = $"http://tizen.org/{ns.ToString().ToLower()}/{item}";
            Information.TryGetValue<T>(uri, out var value);
            return value;
        }
    }
}
