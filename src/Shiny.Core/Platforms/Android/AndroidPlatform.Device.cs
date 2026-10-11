using System;
using System.Globalization;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Content.Res;
using Android.OS;

namespace Shiny;


public partial class AndroidPlatform
{
    GlobalizationMonitor? globalization;
    GlobalizationMonitor Globalization => LazyInitializer.EnsureInitialized(ref this.globalization, () => new(
        this,
        refresh => Watch(Intent.ActionLocaleChanged, refresh),
        refresh => Watch(Intent.ActionTimezoneChanged, refresh)
    ));


    public Version AppVersion
    {
        get
        {
            var pm = this.AppContext.PackageManager!;
            var name = this.AppContext.PackageName!;
            var info = OperatingSystem.IsAndroidVersionAtLeast(33)
                ? pm.GetPackageInfo(name, PackageManager.PackageInfoFlags.Of(0L))
                : pm.GetPackageInfo(name, 0);

            return PlatformVersionParser.Parse(info?.VersionName) ?? new Version(1, 0);
        }
    }

    public string DeviceManufacturer => Build.Manufacturer ?? "Unknown";
    public string DeviceModel => Build.Model ?? "Unknown";
    public Version? PlatformVersion => PlatformVersionParser.Parse(Build.VERSION.Release);
    public string Platform => "Android";

    public DeviceIdiom DeviceIdiom
    {
        get
        {
            var uiMode = this.AppContext.GetSystemService(Context.UiModeService) as UiModeManager;
            switch (uiMode?.CurrentModeType)
            {
                case UiMode.TypeTelevision:
                    return DeviceIdiom.TV;

                case UiMode.TypeCar:
                    return DeviceIdiom.Car;

                case UiMode.TypeWatch:
                    return DeviceIdiom.Watch;

                case UiMode.TypeDesk:
                    return DeviceIdiom.Desktop;
            }

            // the same 600dp line Android's own resource qualifiers (sw600dp) draw for a tablet
            var smallestWidth = this.AppContext.Resources?.Configuration?.SmallestScreenWidthDp ?? 0;
            return smallestWidth >= 600 ? DeviceIdiom.Tablet : DeviceIdiom.Phone;
        }
    }


    public CultureInfo CurrentCulture => this.Globalization.CurrentCulture;
    public event EventHandler<CultureInfo>? CultureChanged
    {
        add => this.Globalization.AddCultureHandler(value);
        remove => this.Globalization.RemoveCultureHandler(value);
    }

    public TimeZoneInfo CurrentTimeZone => this.Globalization.CurrentTimeZone;
    public event EventHandler<TimeZoneInfo>? TimeZoneChanged
    {
        add => this.Globalization.AddTimeZoneHandler(value);
        remove => this.Globalization.RemoveTimeZoneHandler(value);
    }


    public void OpenAppSettings()
    {
        var intent = new Intent(
            Android.Provider.Settings.ActionApplicationDetailsSettings,
            Android.Net.Uri.Parse("package:" + this.AppContext.PackageName)
        );
        intent.AddFlags(ActivityFlags.NewTask);
        this.AppContext.StartActivity(intent);
    }


    IDisposable Watch(string action, Action refresh)
    {
        var receiver = new ActionReceiver(action, refresh);
        var filter = new IntentFilter(action);

        // Android 14+ requires runtime receivers to declare whether they are exported. Locale and time
        // zone changes are protected system broadcasts, so NotExported is correct.
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
            this.AppContext.RegisterReceiver(receiver, filter, ReceiverFlags.NotExported);
        else
            this.AppContext.RegisterReceiver(receiver, filter);

        return new ActionDisposable(() =>
        {
            try
            {
                this.AppContext.UnregisterReceiver(receiver);
            }
            catch
            {
                // IllegalArgumentException when it was never registered - nothing to undo
            }
            receiver.Dispose();
        });
    }


    sealed class ActionReceiver(string action, Action onReceive) : BroadcastReceiver
    {
        public override void OnReceive(Context? context, Intent? intent)
        {
            if (intent?.Action == action)
                onReceive();
        }
    }
}
