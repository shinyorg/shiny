using System;
using Xamarin.Essentials;


namespace Shiny.Integrations.XamEssentials
{
    public class EnvironmentImpl : IEnvironment
    {
        public string AppIdentifier => AppInfo.PackageName;
        public string AppVersion => AppInfo.VersionString;
        public string AppBuild => AppInfo.BuildString;
        public string MachineName => DeviceInfo.Name;
        public string OperatingSystem => DeviceInfo.Platform.ToString();
        public string OperatingSystemVersion => DeviceInfo.VersionString;
        public string Manufacturer => DeviceInfo.Manufacturer;
        public string Model => DeviceInfo.Model;
    }
}
