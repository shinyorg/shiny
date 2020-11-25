using System;
using System.IO;
using System.Reactive.Linq;
using Microsoft.Extensions.DependencyInjection;
using Tizen.Applications;

using DirectoryInfo = System.IO.DirectoryInfo;

namespace Shiny
{
    public class TizenPlatform : IPlatform
    {
        public TizenPlatform()
        {
            this.AppData = new DirectoryInfo(Application.Current.DirectoryInfo.Data);
            this.Cache = new DirectoryInfo(Application.Current.DirectoryInfo.Cache);
            this.Public = new DirectoryInfo(Application.Current.DirectoryInfo.ExternalSharedData);
        }


        public DirectoryInfo AppData { get; }
        public DirectoryInfo Cache { get; }
        public DirectoryInfo Public { get; }

        public string AppIdentifier => this.AppVersion;
        public string AppVersion => Platform.Get<string>("platform.version");
        public string AppBuild => "1";
        public string MachineName => Platform.Get<string>("device_name", PlatformNamespace.Feature);
        public string OperatingSystem => "Tizen";
        public string OperatingSystemVersion => Platform.Get<string>("platform.version");
        public string Manufacturer => Platform.Get<string>("manufacturer", PlatformNamespace.Feature);
        public string Model => Platform.Get<string>("model_name", PlatformNamespace.Feature);


        public void Register(IServiceCollection services) => services.RegisterCommonServices();
        public IObservable<PlatformState> WhenStateChanged() => Observable.Empty<PlatformState>();
    }
}
