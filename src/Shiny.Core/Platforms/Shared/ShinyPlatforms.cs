namespace Shiny
{
    public static class ShinyPlatforms
    {
        public static bool Is(this IPlatform platform, string platformName) => platformName.ToLower() switch
        {
#if __IOS__
            iOS => true,
#elif __ANDROID__
            Android => true,
#elif WINDOWS_UWP
            Uwp => true,
#elif NETSTANDARD
            NetStandard => true,
#endif
            _ => false
        };


        public const string iOS = "ios";
        public const string Android = "android";
        public const string Uwp = "uwp";
        public const string NetStandard = "netstandard";
    }
}
