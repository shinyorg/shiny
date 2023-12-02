using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.PM;

namespace Shiny;


public partial class AndroidPlatform
{
    public int GetNotificationIconResource()
    {
        var id = this.GetResourceIdByName("notification");
        if (id > 0)
            return id;

        id = this.AppContext.ApplicationInfo?.Icon ?? 0;
        if (id > 0)
            return id;

        throw new InvalidOperationException("Unable to find notification icon - ensure you have your application icon set or a drawable resource named notification");
    }


    public TValue GetSystemServiceValue<TValue, TSysType>(string systemTypeName, Func<TSysType, TValue> func) where TSysType : Java.Lang.Object
    {
        using var type = this.GetSystemService<TSysType>(systemTypeName);
        return func(type);
    }


    public Intent CreateIntent<T>(params string[] actions)
    {
        var intent = new Intent(this.AppContext, typeof(T));
        foreach (var action in actions)
            intent.SetAction(action);

        return intent;
    }


    public PendingIntent GetBroadcastPendingIntent<T>(string intentAction, PendingIntentFlags flags, int requestCode = 0, Action<Intent>? modifyIntent = null)
    {
        var intent = this.CreateIntent<T>(intentAction);
        modifyIntent?.Invoke(intent);

        var pendingIntent = PendingIntent.GetBroadcast(
            this.AppContext,
            requestCode,
            intent,
            this.GetPendingIntentFlags(flags)
        );
        return pendingIntent!;
    }


    public PendingIntentFlags GetPendingIntentFlags(PendingIntentFlags flags)
    {
        if (OperatingSystemShim.IsAndroidVersionAtLeast(31) && !flags.HasFlag(PendingIntentFlags.Mutable))
            flags |= PendingIntentFlags.Mutable;

        return flags;
    }


    public T GetSystemService<T>(string key) where T : Java.Lang.Object
        => (T)this.AppContext.GetSystemService(key);


    public void RegisterBroadcastReceiver<T>(bool exported, params string[] actions) where T : BroadcastReceiver, new()
    {
        var receiver = new T();
        var filter = new IntentFilter();
        foreach (var e in actions)
            filter.AddAction(e);

#if ANDROID34_0_OR_GREATER
        if (OperatingSystemShim.IsAndroidVersionAtLeast(34))
        {
            var flags = exported ? ReceiverFlags.Exported : ReceiverFlags.NotExported;
            this.AppContext.RegisterReceiver(receiver, filter, flags);
        }
        else
        {
            this.AppContext.RegisterReceiver(new T(), filter);
        }
#else
        this.AppContext.RegisterReceiver(new T(), filter);
#endif
    }


    public bool IsInManifest(string androidPermission)
    {
        var permissions = this
            .AppContext!
            .PackageManager!
            .GetPackageInfo(
                this.AppContext!.PackageName!,
                PackageInfoFlags.Permissions
            )
            ?.RequestedPermissions;

        if (permissions != null)
        {
            foreach (var permission in permissions)
            {
                if (permission.Equals(androidPermission, StringComparison.InvariantCultureIgnoreCase))
                    return true;
            }
        }
        return false;
    }


    public T GetIntentValue<T>(string intentAction, Func<Intent, T> transform)
    {
        using var filter = new IntentFilter(intentAction);
        using var receiver = this.AppContext.RegisterReceiver(null, filter);
        return transform(receiver!);
    }


    public int GetColorByName(string colorName) => this
        .AppContext
        .Resources
        .GetIdentifier(
            colorName,
            "color",
            this.AppContext.PackageName
        );

    public int GetResourceIdByName(string iconName) => this
        .AppContext
        .Resources
        .GetIdentifier(
            iconName,
            "drawable",
            this.AppContext.PackageName
        );


    // Expects raw resource name like "notify_sound" or "raw/notify_sound"
    public int GetRawResourceIdByName(string rawName) => this
        .AppContext
        .Resources
        .GetIdentifier(
            rawName,
            "raw",
            this.AppContext.PackageName
        );


    public IObservable<PermissionRequestResult> RequestFilteredPermissions(params AndroidPermission[] androidPermissions)
    {
        var list = new List<string>();
        foreach (var p in androidPermissions)
        {
            var meetsMin = p.MinSdkVersion == null || (int)Android.OS.Build.VERSION.SdkInt >= p.MinSdkVersion;
            var meetsMax = p.MaxSdkVersion == null || (int)Android.OS.Build.VERSION.SdkInt <= p.MaxSdkVersion;

            if (meetsMin && meetsMax)
                list.Add(p.Permission);
        }
        return this.RequestPermissions(list.ToArray());
    }


    public bool EnsureAllManifestEntries(params AndroidPermission[] androidPermissions)
    {
        foreach (var p in androidPermissions)
        {
            var meetsMin = p.MinSdkVersion == null || (int)Android.OS.Build.VERSION.SdkInt >= p.MinSdkVersion;
            var meetsMax = p.MaxSdkVersion == null || (int)Android.OS.Build.VERSION.SdkInt <= p.MaxSdkVersion;

            if (meetsMin && meetsMax)
            {
                if (!this.IsInManifest(p.Permission))
                    return false;
            }
        }
        return true;
    }
}
