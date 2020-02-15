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
        public string OperatingSystem => NSProcessInfo.ProcessInfo.OperatingSystemName;
        public string OperatingSystemVersion => NSProcessInfo.ProcessInfo.OperatingSystemVersionString;
        public string Manufacturer { get; } = "Apple";
        public string Model { get; } = "";
    }
}