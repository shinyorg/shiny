using System;


namespace Shiny
{
    public class EnvironmentImpl : IEnvironment
    {
        public string AppIdentifier => this.AppVersion;
        public string AppVersion => Platform.Get<string>("platform.version");
        public string AppBuild => "1";
        public string MachineName => Platform.Get<string>("device_name", PlatformNamespace.Feature);
        public string OperatingSystem => "Tizen";
        public string OperatingSystemVersion => Platform.Get<string>("platform.version");
        public string Manufacturer => Platform.Get<string>("manufacturer", PlatformNamespace.Feature);
        public string Model => Platform.Get<string>("model_name", PlatformNamespace.Feature);
    }
}