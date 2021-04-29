namespace Shiny
{
    public static class ShinyPlatforms
    {
        public static bool Is(this IPlatform platform, string platformName) => platformName.ToLower() switch
        {
#if __IOS__
            iOS => true,
            Android => false,
            Uwp => false,
            _ => false
#elif __ANDROID__
            iOS => false,
            Android => true,
            Uwp => false,
            _ => false
#elif WINDOWS_UWP
            iOS => false,
            Android => false,
            Uwp => true,
            _ => false
#else
            _ => false
#endif
        };


        public const string iOS = "ios";
        public const string Android = "android";
        public const string Uwp = "uwp";
    }
}
