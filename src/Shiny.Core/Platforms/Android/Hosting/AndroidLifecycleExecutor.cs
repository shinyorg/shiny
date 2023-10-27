using System;
using System.Collections.Generic;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using Android.Runtime;
using AndroidX.Lifecycle;
using Java.Interop;
using Microsoft.Extensions.Logging;

namespace Shiny.Hosting;


public class AndroidLifecycleExecutor : Java.Lang.Object, IShinyStartupTask, ILifecycleObserver, IDisposable
{
    readonly ILogger logger;
    readonly AndroidPlatform platform;
    readonly IEnumerable<IAndroidLifecycle.IApplicationLifecycle> appHandlers;
    readonly IEnumerable<IAndroidLifecycle.IOnActivityOnCreate> onCreateHandlers;
    readonly IEnumerable<IAndroidLifecycle.IOnActivityRequestPermissionsResult> permissionHandlers;
    readonly IEnumerable<IAndroidLifecycle.IOnActivityNewIntent> newIntentHandlers;
    readonly IEnumerable<IAndroidLifecycle.IOnActivityResult> activityResultHandlers;


    public AndroidLifecycleExecutor(IntPtr handle, JniHandleOwnership ownership) : base(handle, ownership) { }

    public AndroidLifecycleExecutor(
        ILogger<AndroidLifecycleExecutor> logger,
        AndroidPlatform platform,
        IEnumerable<IAndroidLifecycle.IApplicationLifecycle> appHandlers,
        IEnumerable<IAndroidLifecycle.IOnActivityOnCreate> onCreateHandlers,
        IEnumerable<IAndroidLifecycle.IOnActivityRequestPermissionsResult> permissionHandlers,
        IEnumerable<IAndroidLifecycle.IOnActivityNewIntent> newIntentHandlers,
        IEnumerable<IAndroidLifecycle.IOnActivityResult> activityResultHandlers
    )
    {
        this.logger = logger;
        this.platform = platform;
        this.appHandlers = appHandlers;
        this.onCreateHandlers = onCreateHandlers;
        this.permissionHandlers = permissionHandlers;
        this.newIntentHandlers = newIntentHandlers;
        this.activityResultHandlers = activityResultHandlers;
    }


    public void Start()
    {
        // this is really only need for unit tests - it will passthrough under normal circumstances
        this.platform.InvokeOnMainThread(() =>
        {
            try
            {
                ProcessLifecycleOwner.Get().Lifecycle.AddObserver(this);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Could not attach lifecycle observer");
            }
        });
    }


    [Lifecycle.Event.OnResume]
    [Export]
    public void OnResume() => this.Execute(this.appHandlers, x => x.OnForeground());


    [Lifecycle.Event.OnPause]
    [Export]
    public void OnPause() => this.Execute(this.appHandlers, x => x.OnBackground());

    //[Lifecycle.Event.OnDestroy]
    //[Export]
    //public void OnDestroy()
    //{
    //    Console.WriteLine("LIFECYCLE: OnDestory");
    //}

    public void OnActivityOnCreate(Activity activity, Bundle? savedInstanceState)
        => this.Execute(this.onCreateHandlers, x => x.ActivityOnCreate(activity, savedInstanceState));

    public void OnRequestPermissionsResult(Activity activity, int requestCode, string[] permissions, Permission[] grantResults)
        => this.Execute(this.permissionHandlers, x => x.Handle(activity, requestCode, permissions, grantResults));

    public void OnNewIntent(Activity activity, Intent? intent)
        => this.Execute(this.newIntentHandlers, x => x.Handle(activity, intent));

    public void OnActivityResult(Activity activity, int requestCode, Result result, Intent? intent)
        => this.Execute(this.activityResultHandlers, x => x.Handle(activity, requestCode, result, intent));

    public new void Dispose()
    {
        // dispose is (should) only used by unit tests
        // this is really only need for unit tests - it will passthrough under normal circumstances
        this.platform.InvokeOnMainThread(() =>
        {
            try
            {
                ProcessLifecycleOwner.Get().Lifecycle.RemoveObserver(this);
            }
            catch (Exception ex)
            {
                this.logger.LogWarning(ex, "Could not remove lifecycle observer");
            }
        });
        base.Dispose();
    }


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
                this.logger.LogError(ex, "Failed to execute lifecycle call");
            }
        }
    }
}
