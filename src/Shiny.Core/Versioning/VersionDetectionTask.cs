using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;

using Shiny.Settings;


namespace Shiny.Versioning
{
    public class VersionDetectionTask : IShinyStartupTask
    {
        readonly IPlatform platform;
        readonly ISettings settings;
        readonly IEnumerable<IVersionChangeDelegate> delegates;


        public VersionDetectionTask(IPlatform platform,
                                       ISettings settings,
                                       IEnumerable<IVersionChangeDelegate> delegates)
        {
            this.platform = platform;
            this.settings = settings;
            this.delegates = delegates;
        }


        public async void Start()
        {
            await Task.WhenAll(
                this.TestOS(),
                this.TestLibrary(),
                this.TestAppVersion()
            );
        }


        Task TestOS() => this.Test(
            "LastOperatingSystem",
            this.platform.OperatingSystemVersion,
            (del, oldVersion, newVersion) => del.OnOperatingSystemVersionChanged(oldVersion, newVersion)
        );


        Task TestLibrary() => this.Test(
            "ShinyVersion",
            this.GetType().Assembly.GetName().Version.ToString(),
            (del, oldVersion, newVersion) => del.OnShinyLibraryChanged(oldVersion, newVersion)
        );


        Task TestAppVersion() => this.Test(
            "AppVersion",
            this.platform.AppVersion,
            (del, oldVersion, newVersion) => del.OnAppVersionChanged(oldVersion, newVersion)
        );


        async Task Test(string settingsKey, string currentVersion, Func<IVersionChangeDelegate, string?, string, Task> execute)
        {
            var lastVersion = this.settings.Get<string?>(settingsKey);

            if (lastVersion == null || lastVersion.Equals(currentVersion))
            {
                await this.delegates.RunDelegates(x => execute(x, lastVersion, currentVersion));
                this.settings.Set(settingsKey, currentVersion);
            }
        }
    }
}
