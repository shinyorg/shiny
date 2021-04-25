namespace Docs.Shortcodes
{
    public class Package
    {
        public string Name { get; set; }
        public string Description { get; set; }
        public string? Category { get; set; }

        public PackageService[] Services { get; set; }
    }


    public class PackageService
    {
        public string Name { get; set; }
        public string Static { get; set; }
        public string? Startup { get; set; }

        public string? BgDelegate { get; set; }
        public bool? BgDelegateRequired { get; set; }

        public PlatformSupport[] Platforms { get; set; }
    }


    public class PlatformSupport
    {
        public Platform Platform { get; set; }
        public string MinVersion { get; set; }
        public bool RequiresConfig { get; set; }
    }


    public enum Platform
    {
        iOS,
        Android,
        Uwp
    }
}
