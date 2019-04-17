using System;
using B = global::Android.OS.Build;


namespace Shiny
{
    public class EnvironmentImpl : IEnvironment
    {
        readonly AndroidContext context;
        public EnvironmentImpl(AndroidContext context) => this.context = context;


        public string AppIdentifier => this.context.AppContext.PackageName;
        public string AppVersion => this.context.Package.VersionName;
        public string AppBuild => this.context.Package.VersionCode.ToString();

        public string MachineName => "Android";
        public string OperatingSystem => B.VERSION.Release;
        public string Manufacturer => B.Manufacturer;
        public string Model => B.Model;
    }
}
