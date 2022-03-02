using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.PM;


namespace Shiny
{
    public static class AndroidExtensions
    {
        public static IObservable<PermissionRequestResult> RequestFilteredPermissions(this IAndroidContext context, params AndroidPermission[] androidPermissions)
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


        public static bool EnsureAllManifestEntries(this IAndroidContext context, params AndroidPermission[] androidPermissions)
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


        public static IObservable<(Result Result, Intent Data)> RequestActivityResult(this IAndroidContext context, Intent intent)
            => context.RequestActivityResult((requestCode, activity) =>
                activity.StartActivityForResult(intent, requestCode)
            );


        public static void ShinyOnRequestPermissionsResult(this Activity activity, int requestCode, string[] permissions, Permission[] grantResults)
            => ShinyHost
                .Resolve<IAndroidContext>()
                .OnRequestPermissionsResult(requestCode, permissions, grantResults);

        public static void ShinyOnActivityResult(this Activity activity, int requestCode, Result resultCode, Intent data)
            => ShinyHost
                .Resolve<IAndroidContext>()
                .OnActivityResult(requestCode, resultCode, data);

        public static void ShinyOnCreate(this Application application, IShinyStartup? startup = null)
            => ShinyHost.Init(new AndroidPlatform(application), startup);

        public static void ShinyOnCreate(this Activity activity)
            => activity.ShinyOnNewIntent(activity.Intent);

        public static void ShinyOnNewIntent(this Activity activity, Intent? intent)
            => ShinyHost.Resolve<IAndroidContext>()?.OnNewIntent(intent);

        //public static IObservable<(Result Result, Intent Data)> RequestActivityResult<TActivity>(this IAndroidContext context) where TActivity : Activity
        //    => context.RequestActivityResult((requestCode, activity) =>
        //        activity.StartActivityForResult(typeof(TActivity), requestCode)
        //    );
    }
}
