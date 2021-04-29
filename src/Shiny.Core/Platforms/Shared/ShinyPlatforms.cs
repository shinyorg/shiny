namespace Shiny
{
    public static class ShinyPlatforms
    {
        public static string CurrentPlatform()
        {
#if __IOS__
            return iOS;
#elif __ANDROID__
            return Android;
#elif WINDOWS_UWP
            return Uwp;
#elif NETSTANDARD
            return NetStandard;
#else
            return null;
#endif
        }


        public static string CurrentPlatform(this IPlatform platform) => CurrentPlatform();


        public static bool Is(string platformName) => platformName.ToLower() switch
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

        public static bool Is(this IPlatform platform, string platformName) => Is(platformName);
        public static bool IsAndroid(this IPlatform platform) => Is(Android);
        public static bool IsAndroid() => Is(Android);

        public static bool IsIos(this IPlatform platform) => Is(iOS);
        public static bool IsIos() => Is(iOS);

        public static bool IsUwp(this IPlatform platform) => Is(Uwp);
        public static bool IsUwp() => Is(Uwp);

        public static bool IsNetstandard(this IPlatform platform) => platform.Is(NetStandard);
        public static bool IsNetstandard() => Is(NetStandard);

        public const string iOS = "ios";
        public const string Android = "android";
        public const string Uwp = "uwp";
        public const string NetStandard = "netstandard";
    }
}
