using System;
using System.Reactive.Linq;
using Windows.Security.ExchangeActiveSyncProvisioning;
using Windows.ApplicationModel;
using Windows.UI.WebUI;


namespace Shiny.Platforms.Uwp
{
    public class UwpPlatform : IPlatform
    {
        readonly EasClientDeviceInformation deviceInfo = new EasClientDeviceInformation();
        public string AppIdentifier => Package.Current.Id.Name;
        public string AppVersion => $"{Package.Current.Id.Version.Major}.{Package.Current.Id.Version.Minor}.{Package.Current.Id.Version.Build}.{Package.Current.Id.Version.Revision}";
        public string AppBuild => Package.Current.Id.Version.Build.ToString();

        public string Manufacturer => this.deviceInfo.SystemManufacturer;
        public string Model => this.deviceInfo.SystemSku;
        public string OperatingSystem => "Windows"; //this.deviceInfo.OperatingSystem;
        public string OperatingSystemVersion => "";
        public string MachineName => "";


        //https://docs.microsoft.com/en-us/uwp/api/Windows.UI.Xaml.Application?view=winrt-19041
        public IObservable<PlatformState> WhenStateChanged() => Observable.Create<PlatformState>(ob =>
        {
            var fgHandler = new LeavingBackgroundEventHandler((sender, target) => ob.OnNext(PlatformState.Foreground));
            var bgHandler = new EnteredBackgroundEventHandler((sender, target) => ob.OnNext(PlatformState.Background));

            if (this.app == null)
            {
                ob.OnNext(PlatformState.Background);
            }
            else
            {
                // TODO: application will be normal if launched from background
                this.app.LeavingBackground += fgHandler;
                this.app.EnteredBackground += bgHandler;
            }
            return () =>
            {
                if (this.app != null)
                {
                    this.app.LeavingBackground -= fgHandler;
                    this.app.EnteredBackground -= bgHandler;
                }
            };
        });
    }
}
