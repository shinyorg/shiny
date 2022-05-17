using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.PM;
using AndroidX.Lifecycle;
using Java.Interop;
using Microsoft.Extensions.Logging;

namespace Shiny.Hosting;


public class AndroidLifecycleExecutor : Java.Lang.Object, ILifecycleObserver, IDisposable
{
    readonly ILogger logger;
    readonly IEnumerable<IAndroidLifecycle.IApplicationLifecycle> appHandlers;
    readonly IEnumerable<IAndroidLifecycle.IOnActivityRequestPermissionsResult> permissionHandlers;
    readonly IEnumerable<IAndroidLifecycle.IOnActivityNewIntent> newIntentHandlers;
    readonly IEnumerable<IAndroidLifecycle.IOnActivityResult> activityResultHandlers;


    public AndroidLifecycleExecutor(
        ILogger<AndroidLifecycleExecutor> logger,
        IEnumerable<IAndroidLifecycle.IApplicationLifecycle> appHandlers,
        IEnumerable<IAndroidLifecycle.IOnActivityRequestPermissionsResult> permissionHandlers,
        IEnumerable<IAndroidLifecycle.IOnActivityNewIntent> newIntentHandlers,
        IEnumerable<IAndroidLifecycle.IOnActivityResult> activityResultHandlers
    )
    {
        this.logger = logger;
        this.appHandlers = appHandlers;
        this.permissionHandlers = permissionHandlers;
        this.newIntentHandlers = newIntentHandlers;
        this.activityResultHandlers = activityResultHandlers;

        ProcessLifecycleOwner.Get().Lifecycle.AddObserver(this);
    }


    [Lifecycle.Event.OnResume]
    [Export]
    public void OnResume()
    {
    }


    [Lifecycle.Event.OnPause]
    [Export]
    public void OnPause()
    {
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


    public void OnAppForegrounding()
    {

    }

    public void OnAppBackgrounding()
    {

    }

    public void Dispose()
    {
        ProcessLifecycleOwner.Get().Lifecycle.RemoveObserver(this);
    }
}
