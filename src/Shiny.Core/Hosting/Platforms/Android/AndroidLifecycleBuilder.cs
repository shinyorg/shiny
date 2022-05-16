using System;
using System.Reactive.Subjects;
using Android.App;
using Android.OS;
using Android.Runtime;

namespace Shiny.Hosting;


public static class AndroidLifecycleExtensions
{
    public static ILifecycleBuilder AddAndroid(this ILifecycleBuilder lifecycleBuilder, Action<AndroidLifecycleBuilder> builder)
    {

        return lifecycleBuilder;
    }
}


[Preserve(AllMembers = true)]
public class AndroidLifecycleBuilder 
{

    // TODO: only activity OnNewIntent & OnPermissionRequest need to be routed from the activity directly - the activity lifecycle callbacks can handle the rest



    // TODO: events needed from the activity instance directly
    //public static void ShinyOnRequestPermissionsResult(this Activity activity, int requestCode, string[] permissions, Permission[] grantResults)
    //    => ShinyHost
    //        .Resolve<IPlatform>()
    //        .OnRequestPermissionsResult(requestCode, permissions, grantResults);

    //public static void ShinyOnActivityResult(this Activity activity, int requestCode, Result resultCode, Intent data)
    //    => ShinyHost
    //        .Resolve<IPlatform>()
    //        .OnActivityResult(requestCode, resultCode, data);

    //// no longer used
    //[Obsolete("No longer needed")]
    //public static void ShinyOnCreate(this Activity activity) { }
    ////=> activity.ShinyOnNewIntent(activity.Intent);

    //public static void ShinyOnNewIntent(this Activity activity, Intent? intent)
    //    => ShinyHost.Resolve<IPlatform>()?.OnNewIntent(intent);

    //public static IObservable<(Result Result, Intent Data)> RequestActivityResult<TActivity>(this IAndroidContext context) where TActivity : Activity
    //    => context.RequestActivityResult((requestCode, activity) =>
    //        activity.StartActivityForResult(typeof(TActivity), requestCode)
    //    );

    //public record ActivityResult(Activity Activity, Result Result, Intent Intent);
    //public record ActivityPermissionResult(Activity Activity);

}
