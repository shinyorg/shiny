using System;
using Foundation;
using UIKit;

namespace Shiny
{
    // this is startable so the properties can be set on the main thread - this is mostly an iOS thing
    public class EnvironmentImpl : IEnvironment, IShinyStartupTask
    {
        public string AppIdentifier { get; private set; }
        public string AppVersion { get; private set; }
        public string AppBuild { get; private set; }

        public string MachineName { get; } = "";
        public string OperatingSystem { get; private set; }
        public string OperatingSystemVersion { get; private set; }
        public string Manufacturer { get; } = "Apple";
        public string Model { get; }


        public void Start()
        {
            //this.operatingSystem = UIDevice.CurrentDevice.SystemName;
            this.AppIdentifier = NSBundle.MainBundle.BundleIdentifier;
            this.AppVersion = NSBundle.MainBundle.InfoDictionary["CFBundleVersion"].ToString();
            this.AppBuild = NSBundle.MainBundle.InfoDictionary["CFBundleShortVersionString"].ToString();
#if __IOS__
            this.OperatingSystem = "iOS";
            this.OperatingSystemVersion = UIDevice.CurrentDevice.SystemVersion;
#elif __TVSOS__
            this.OperatingSystem = "tvOS";
            this.OperatingSystemVersion = UIDevice.CurrentDevice.SystemVersion;
#elif __WATCHOS__
            this.OperatingSystem = "watchOS";
            this.OperatingSystemVersion = "1";
#endif

        }
    }
}