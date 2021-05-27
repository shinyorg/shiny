using System;
using Android.App;
using Android.Content;


namespace Shiny
{
    public static class PlatformExtensions
    {
        public static IObservable<(Result Result, Intent Data)> RequestActivityResult(this IAndroidContext context, Intent intent)
            => context.RequestActivityResult((requestCode, activity) =>
                activity.StartActivityForResult(intent, requestCode)
            );


        //public static IObservable<(Result Result, Intent Data)> RequestActivityResult<TActivity>(this IAndroidContext context) where TActivity : Activity
        //    => context.RequestActivityResult((requestCode, activity) =>
        //        activity.StartActivityForResult(typeof(TActivity), requestCode)
        //    );
    }
}
