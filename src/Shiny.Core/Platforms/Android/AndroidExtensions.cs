using System;
using System.Collections.Generic;
using System.Reactive.Linq;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Shiny.Hosting;

namespace Shiny;


public static class AndroidExtensions
{
    public static PendingIntent GetBroadcastPendingIntent<T>(this IAndroidHost platform, string intentAction, PendingIntentFlags flags, int requestCode = 0, Action<Intent>? modifyIntent = null)
    {
        var intent = platform.CreateIntent<T>(intentAction);
        modifyIntent?.Invoke(intent);

        var pendingIntent = PendingIntent.GetBroadcast(
            platform.AppContext,
            requestCode,
            intent,
            platform.GetPendingIntentFlags(flags)
        );
        return pendingIntent!;
    }


    public static PendingIntentFlags GetPendingIntentFlags(this IAndroidHost platform, PendingIntentFlags flags)
    {
        if (platform.IsMinApiLevel(31) && !flags.HasFlag(PendingIntentFlags.Mutable))
            flags |= PendingIntentFlags.Mutable;

        return flags;
    }


    public static T GetSystemService<T>(this IAndroidHost platform, string key) where T : Java.Lang.Object
        => (T)platform.AppContext.GetSystemService(key);


    public static void RegisterBroadcastReceiver<T>(this IAndroidHost platform, params string[] actions) where T : BroadcastReceiver, new()
    {
        var filter = new IntentFilter();
        foreach (var e in actions)
            filter.AddAction(e);

        platform.AppContext.RegisterReceiver(new T(), filter);
    }


    public static TValue GetSystemServiceValue<TValue, TSysType>(this IAndroidHost platform, string systemTypeName, Func<TSysType, TValue> func) where TSysType : Java.Lang.Object
    {
        using (var type = platform.GetSystemService<TSysType>(systemTypeName))
        {
            return func(type);
        }
    }


    public static Intent CreateIntent<T>(this IAndroidHost platform, params string[] actions)
    {
        var intent = new Intent(platform.AppContext, typeof(T));
        foreach (var action in actions)
            intent.SetAction(action);

        return intent;
    }


    public static bool IsInManifest(this IAndroidHost platform, string androidPermission)
    {
        var permissions = platform
            .AppContext!
            .PackageManager!
            .GetPackageInfo(
                platform.AppContext!.PackageName!,
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
        //Log.Write("Permissions", $"You need to declare the '{androidPermission}' in your AndroidManifest.xml");
        return false;
    }


    public static IObservable<Intent> WhenIntentReceived(this IAndroidHost platform, string intentAction)
        => Observable.Create<Intent>(ob =>
        {
            var filter = new IntentFilter();
            filter.AddAction(intentAction);
            var receiver = new ObservableBroadcastReceiver
            {
                OnEvent = ob.OnNext
            };
            platform.AppContext.RegisterReceiver(receiver, filter);
            return () => platform.AppContext.UnregisterReceiver(receiver);
        });



    public static T GetIntentValue<T>(this IAndroidHost platform, string intentAction, Func<Intent, T> transform)
    {
        using var filter = new IntentFilter(intentAction);
        using var receiver = platform.AppContext.RegisterReceiver(null, filter);
        return transform(receiver!);
    }


    public static int GetColorByName(this IAndroidHost platform, string colorName) => platform
        .AppContext
        .Resources
        .GetIdentifier(
            colorName,
            "color",
            platform.AppContext.PackageName
        );

    public static int GetResourceIdByName(this IAndroidHost platform, string iconName) => platform
        .AppContext
        .Resources
        .GetIdentifier(
            iconName,
            "drawable",
            platform.AppContext.PackageName
        );


    // Expects raw resource name like "notify_sound" or "raw/notify_sound"
    public static int GetRawResourceIdByName(this IAndroidHost platform, string rawName) => platform
        .AppContext
        .Resources
        .GetIdentifier(
            rawName,
            "raw",
            platform.AppContext.PackageName
        );


    public static IObservable<PermissionRequestResult> RequestFilteredPermissions(this IAndroidHost context, params AndroidPermission[] androidPermissions)
    {
        var list = new List<string>();
        foreach (var p in androidPermissions)
        {
            var meetsMin = p.MinSdkVersion == null || (int)Android.OS.Build.VERSION.SdkInt >= p.MinSdkVersion;
            var meetsMax = p.MaxSdkVersion == null || (int)Android.OS.Build.VERSION.SdkInt <= p.MaxSdkVersion;

            if (meetsMin && meetsMax)
                list.Add(p.Permission);
        }
        return context.RequestPermissions(list.ToArray());
    }


    public static bool EnsureAllManifestEntries(this IAndroidHost context, params AndroidPermission[] androidPermissions)
    {
        foreach (var p in androidPermissions)
        {
            var meetsMin = p.MinSdkVersion == null || (int)Android.OS.Build.VERSION.SdkInt >= p.MinSdkVersion;
            var meetsMax = p.MaxSdkVersion == null || (int)Android.OS.Build.VERSION.SdkInt <= p.MaxSdkVersion;

            if (meetsMin && meetsMax)
            {
                if (!context.IsInManifest(p.Permission))
                    return false;
            }
        }
        return true;
    }


    public static IObservable<(Result Result, Intent Data)> RequestActivityResult(this IAndroidHost context, Intent intent)
        => context.RequestActivityResult((requestCode, activity) =>
            activity.StartActivityForResult(intent, requestCode)
        );



}
