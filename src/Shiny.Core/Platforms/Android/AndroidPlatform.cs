using System;
using System.Collections.Concurrent;
using System.IO;
using System.Linq;
using System.Collections.Generic;
using System.Threading;
using System.Threading.Tasks;
using Android;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.Content;
using AndroidX.Core.App;
using Microsoft.Extensions.DependencyInjection;
using Shiny.Extensions.Stores;
using Shiny.Hosting;

namespace Shiny;


public partial class AndroidPlatform : IPlatform,
                                       IAndroidLifecycle.IOnActivityRequestPermissionsResult,
                                       IAndroidLifecycle.IOnActivityResult
{
    const string PermissionsKey = nameof(PermissionsKey);
    int requestCode;
    readonly List<string> requestedPermissions;
    readonly ConcurrentDictionary<int, TaskCompletionSource<PermissionRequestResult>> pendingPermissions = new();

    static AndroidActivityLifecycle activityLifecycle; // this should never change once installed on the platform
    readonly IKeyValueStore store;

    public AndroidPlatform([FromKeyedServices(StoreKeys.Default)] IKeyValueStore store)
    {
        var app = (Application)Application.Context;
        activityLifecycle ??= new(app);
        this.AppContext = app;
        this.AppData = new DirectoryInfo(this.AppContext.FilesDir.AbsolutePath);
        this.Cache = new DirectoryInfo(this.AppContext.CacheDir.AbsolutePath);
        var publicDir = this.AppContext.GetExternalFilesDir(null);
        if (publicDir != null)
            this.Public = new DirectoryInfo(publicDir.AbsolutePath);

        this.store = store;
        this.requestedPermissions = this.store.Get<List<string>>(PermissionsKey) ?? new List<string>();
    }


    public AccessState GetCurrentPermissionStatus(string androidPermission)
    {
        var self = ContextCompat.CheckSelfPermission(this.AppContext, androidPermission);
        if (self == Permission.Granted)
            return AccessState.Available;

        if (!this.HasRequestedPermission(androidPermission))
            return AccessState.Unknown;

        return AccessState.Denied;
    }

    // lifecycle hooks
    public void Handle(Activity activity, int requestCode, string[] permissions, Permission[] grantResults)
    {
        if (this.pendingPermissions.TryRemove(requestCode, out var tcs))
            tcs.TrySetResult(new PermissionRequestResult(requestCode, permissions, grantResults));
    }

    public void Handle(Activity activity, int requestCode, Result resultCode, Intent data) { }


    public Application AppContext { get; }
    public DirectoryInfo AppData { get; }
    public DirectoryInfo Cache { get; }
    public DirectoryInfo Public { get; }


    public Activity? CurrentActivity => activityLifecycle.Activity;

    public event EventHandler<ActivityChanged> ActivityChanged
    {
        add => activityLifecycle.ActivityChanged += value;
        remove => activityLifecycle.ActivityChanged -= value;
    }

    public Task<ActivityChanged> WaitForActivity(ActivityState state = ActivityState.Resumed, CancellationToken cancellationToken = default)
    {

        var tcs = new TaskCompletionSource<ActivityChanged>();
        EventHandler<ActivityChanged>? handler = null;
        handler = (_, e) =>
        {
            if (e.State != state)
                return;
            activityLifecycle.ActivityChanged -= handler;
            tcs.TrySetResult(e);
        };
        activityLifecycle.ActivityChanged += handler;

        if (cancellationToken.CanBeCanceled)
            cancellationToken.Register(() =>
            {
                activityLifecycle.ActivityChanged -= handler;
                tcs.TrySetCanceled(cancellationToken);
            });

        return tcs.Task;
    }


    readonly Handler handler = new Handler(Looper.MainLooper);
    public void InvokeOnMainThread(Action action)
    {
        if (Looper.MainLooper.IsCurrentThread)
            action();
        else
            this.handler.Post(action);
    }


    public async Task<AccessState> RequestForegroundServicePermissions()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var results = await this.RequestPermissions(
                Manifest.Permission.ForegroundService,
                Manifest.Permission.PostNotifications
            );
            if (results.IsSuccess())
                return AccessState.Available;

            if (!results.IsGranted(Manifest.Permission.ForegroundService))
                return AccessState.NotSetup;

            return AccessState.Restricted; // no post_notifications
        }
        else if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            var results = await this.RequestPermissions(Manifest.Permission.ForegroundService);
            if (results.IsSuccess())
                return AccessState.Available;

            return AccessState.NotSetup;
        }

        return AccessState.Available;
    }

    public const string ActionServiceStart = "ACTION_START_FOREGROUND_SERVICE";
    public const string ActionServiceStop = "ACTION_STOP_FOREGROUND_SERVICE";
    public const string IntentActionStopWithTask = "StopWithTask";

    public void StartService(Type serviceType, bool stopWithTask = true)
    {
        var intent = new Intent(this.AppContext, serviceType);
        intent.SetAction(ActionServiceStart);
        intent.PutExtra(IntentActionStopWithTask, stopWithTask);

        if (OperatingSystem.IsAndroidVersionAtLeast(31))
            this.AppContext.StartForegroundService(intent);
        else
            this.AppContext.StartService(intent);
    }


    public void StopService(Type serviceType)
    {
        var intent = new Intent(this.AppContext, serviceType);
        intent.SetAction(ActionServiceStop);
        this.AppContext.StartService(intent);
    }


    public int GetDrawableByName(string name) => this
        .AppContext
        .Resources!
        .GetIdentifier(
            name,
            "drawable",
            this.AppContext.PackageName
        );

    public async Task<AccessState> RequestAccess(string androidPermission)
    {
        var result = await this.RequestPermissions(androidPermission).ConfigureAwait(false);
        return result.IsSuccess() ? AccessState.Available : AccessState.Denied;
    }


    public Task<PermissionRequestResult> RequestPermissions(params string[] androidPermissions)
        => this.RequestPermissions(CancellationToken.None, androidPermissions);


    public async Task<PermissionRequestResult> RequestPermissions(CancellationToken cancellationToken, params string[] androidPermissions)
    {
        var allGood = androidPermissions.All(p => ContextCompat.CheckSelfPermission(this.AppContext, p) == Permission.Granted);
        if (allGood)
        {
            var grants = Enumerable.Repeat(Permission.Granted, androidPermissions.Length).ToArray();
            return new PermissionRequestResult(0, androidPermissions, grants);
        }

        this.SetRequestedPermissions(androidPermissions);
        var current = Interlocked.Increment(ref this.requestCode);

        var tcs = new TaskCompletionSource<PermissionRequestResult>();
        this.pendingPermissions[current] = tcs;

        using var cts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
        cts.CancelAfter(TimeSpan.FromSeconds(5));
        cts.Token.Register(() =>
        {
            if (this.pendingPermissions.TryRemove(current, out var t))
                t.TrySetException(new TimeoutException("A current activity was not detected to be able to request permissions"));
        });

        if (this.CurrentActivity != null)
        {
            ActivityCompat.RequestPermissions(this.CurrentActivity, androidPermissions, current);
        }
        else
        {
            EventHandler<ActivityChanged>? actHandler = null;
            actHandler = (_, x) =>
            {
                activityLifecycle.ActivityChanged -= actHandler;
                ActivityCompat.RequestPermissions(x.Activity, androidPermissions, current);
            };
            activityLifecycle.ActivityChanged += actHandler;
        }

        return await tcs.Task.ConfigureAwait(false);
    }


    void SetRequestedPermissions(string[] androidPermissions)
    {
        lock (this.requestedPermissions)
        {
            var count = this.requestedPermissions.Count;
            foreach (var p in androidPermissions)
            {
                if (!this.requestedPermissions.Contains(p, StringComparer.InvariantCultureIgnoreCase))
                    this.requestedPermissions.Add(p);
            }
            if (count != this.requestedPermissions.Count)
                this.store.Set(PermissionsKey, this.requestedPermissions);
        }
    }


    bool HasRequestedPermission(string androidPermission)
    {
        lock (this.requestedPermissions)
        {
            return this.requestedPermissions.Contains(
                androidPermission,
                StringComparer.InvariantCultureIgnoreCase
            );
        }
    }


}
