using System;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.ApplicationModel;


namespace Shiny
{
    public class EnvironmentImpl : IEnvironment
    {
        readonly EasClientDeviceInformation deviceInfo = new EasClientDeviceInformation();

//public override string BundleName => Package.Current.Id.Identifier;
        //public override string Version { get; } = $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}.{Package.Current.Id.Version.Revision}";
        //public override string ShortVersion { get; } = $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}";

        public string AppIdentifier => Package.Current.Id.Name;
        public string AppVersion => $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}.{Package.Current.Id.Version.Revision}";
        public string AppBuild { get; }

        public string Manufacturer => this.deviceInfo.SystemManufacturer;
        public string Model => this.deviceInfo.SystemSku;
        public string OperatingSystem => "Windows"; //this.deviceInfo.OperatingSystem;

        public string MachineName => "";
    }
}
