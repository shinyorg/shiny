using System;
using System.Collections.Generic;
using System.Threading.Tasks;
using Shiny.Settings;


namespace Shiny.Versioning
{
    public interface IVersionManager
    {
        bool HasOSVersionChanged { get; }
        bool IsInitialInstallDetected { get; }
        bool HasAppVersionChanged { get; }
    }


    public class VersionManager : IVersionManager, IShinyStartupTask
    {
        readonly IPlatform platform;
        readonly ISettings settings;
        readonly IEnumerable<IVersionChangeDelegate> delegates;


        public bool HasOSVersionChanged { get; private set; }
        public bool IsInitialInstallDetected { get; private set; }
        public bool HasAppVersionChanged { get; private set; }
        //public bool HasShinyLibraryChanged { get; private set; }


        public VersionManager(IPlatform platform,
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
                //this.TestLibrary(),
                this.TestAppVersion()
            );
        }


        Task TestOS() => this.Test(
            "LastOperatingSystem",
            this.platform.OperatingSystemVersion,
            async (del, oldVersion, newVersion) =>
            {
                if (oldVersion != null)
                {
                    this.HasOSVersionChanged = true;
                    await del.OnOperatingSystemVersionChanged(oldVersion, newVersion);
                }
            }
        );


        //Task TestLibrary() => this.Test(
        //    "ShinyVersion",
        //    this.GetType().Assembly.GetName().Version.ToString(),
        //    (del, oldVersion, newVersion) =>
        //    {
        //        this.HasShinyLibraryChanged = true;
        //        return del.OnShinyLibraryChanged(oldVersion, newVersion);
        //    }
        //);


        Task TestAppVersion() => this.Test(
            "AppVersion",
            this.platform.AppVersion,
            (del, oldVersion, newVersion) =>
            {
                this.HasOSVersionChanged = true;
                this.IsInitialInstallDetected = oldVersion == null;
                return del.OnAppVersionChanged(oldVersion, newVersion);
            }
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
