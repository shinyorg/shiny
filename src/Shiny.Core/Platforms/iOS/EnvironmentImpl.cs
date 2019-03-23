using System;
using Foundation;

namespace Shiny
{
    public class EnvironmentImpl : IEnvironment
    {
        public string AppIdentifier => NSBundle.MainBundle.BundleIdentifier;
        public string AppVersion => NSBundle.MainBundle.InfoDictionary["CFBundleVersion"].ToString();
        public string AppBuild => NSBundle.MainBundle.InfoDictionary["CFBundleShortVersionString"].ToString();

        public string MachineName { get; } = "";
        public string OperatingSystem { get; } = "iOS";
        public string Manufacturer { get; } = "Apple";
        public string Model { get; }
    }
}
/*
 need to be on UI thread
this.operatingSystem = UIDevice.CurrentDevice.SystemName;
this.operatingSystemVersion = UIDevice.CurrentDevice.SystemVersion;
this.tablet = UIDevice.CurrentDevice.UserInterfaceIdiom == UIUserInterfaceIdiom.Pad;
this.simulator = Runtime.Arch == Arch.SIMULATOR;

 */