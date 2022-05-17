using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Microsoft.Extensions.Logging;

namespace Shiny.Hosting;


public class AndroidLifecycleExecutor
{
    readonly ILogger logger;
    readonly IEnumerable<IAndroidLifecycle.IOnActivityRequestPermissionsResult> permissionListeners;
    readonly IEnumerable<IAndroidLifecycle.IOnActivityNewIntent> newIntentListeners;



    public AndroidLifecycleExecutor(
        ILogger<AndroidLifecycleExecutor> logger,
        IEnumerable<IAndroidLifecycle.IOnActivityRequestPermissionsResult> permissionListeners,
        IEnumerable<IAndroidLifecycle.IOnActivityNewIntent> newIntentListeners,
        IEnumerable<IAndroidLifecycle.IOnActivityResult> activityResultListeners
    )
    {
        this.logger = logger;
    }


    public void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResults)
    {

    }


    public void OnNewIntent(Intent? intent)
    {

    }


    public void OnActivityResult(int requestCode, Result result, Intent? intent)
    {

    }
}
