using System;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Microsoft.Extensions.DependencyInjection;


namespace Shiny
{
    public static class AndroidHooks
    {
        public static void ShinyOnRequestPermissionsResult(this Activity activity, int requestCode, string[] permissions, Permission[] grantResults)
            => ShinyHost
                .Resolve<AndroidContext>()
                .OnRequestPermissionsResult(requestCode, permissions, grantResults);

        public static void ShinyOnCreate(this Application application, IShinyStartup? startup = null, Action<IServiceCollection>? platformBuild = null)
            => ShinyHost.Init(new AndroidPlatformInitializer(application), startup, platformBuild);

        public static void ShinyOnCreate(this Activity activity)
            => activity.ShinyOnNewIntent(activity.Intent);

        public static void ShinyOnNewIntent(this Activity activity, Intent intent)
        {
            if (intent != null)
                ShinyHost.Resolve<AndroidContext>()?.IntentSubject.OnNext(intent);
        }
    }
}
