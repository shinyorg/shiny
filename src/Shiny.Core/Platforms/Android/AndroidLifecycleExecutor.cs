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
        => this.Execute(this.appHandlers, x => x.OnForeground());

    [Lifecycle.Event.OnPause]
    [Export]
    public void OnPause() 
        => this.Execute(this.appHandlers, x => x.OnBackground());

    public void OnRequestPermissionsResult(Activity activity, int requestCode, string[] permissions, Permission[] grantResults)
        => this.Execute(this.permissionHandlers, x => x.Handle(activity, requestCode, permissions, grantResults));

    public void OnNewIntent(Activity activity, Intent? intent)
        => this.Execute(this.newIntentHandlers, x => x.Handle(activity, intent));

    public void OnActivityResult(Activity activity, int requestCode, Result result, Intent? intent)
        => this.Execute(this.activityResultHandlers, x => x.Handle(activity, requestCode, result, intent));

    public void Dispose()
        => ProcessLifecycleOwner.Get().Lifecycle.RemoveObserver(this);


    void Execute<T>(IEnumerable<T> services, Action<T> action)
    {
        foreach (var handler in services)
        {
            try
            {
                action(handler);
            }
            catch (Exception ex)
            {
                this.logger.LogError("Failed to execute lifecycle call", ex);
            }
        }
    }
}
