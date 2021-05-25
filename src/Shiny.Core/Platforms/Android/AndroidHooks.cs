using System;
using Android.App;
using Android.Content;
using Android.Content.PM;


namespace Shiny
{
    public static class AndroidHooks
    {
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
    }
}
