using System;
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
using Shiny.Hosting;
using Shiny.Stores;
using Shiny.Stores.Impl;

namespace Shiny;


public partial class AndroidPlatform : IPlatform,
                                       IAndroidLifecycle.IOnActivityRequestPermissionsResult,
                                       IAndroidLifecycle.IOnActivityResult
{
    const string PermissionsKey = nameof(PermissionsKey);
    int requestCode;
    readonly List<string> requestedPermissions;

    static AndroidActivityLifecycle activityLifecycle; // this should never change once installed on the platform
    readonly ShinySubject<PermissionRequestResult> permissionSubject = new();
    readonly ShinySubject<(int RequestCode, Result Result, Intent Intent)> activityResultSubject = new();
    readonly SettingsKeyValueStore store;

    public AndroidPlatform()
    {
        var app = (Application)Application.Context;
        activityLifecycle ??= new(app);
        this.AppContext = app;
        this.AppData = new DirectoryInfo(this.AppContext.FilesDir.AbsolutePath);
        this.Cache = new DirectoryInfo(this.AppContext.CacheDir.AbsolutePath);
        var publicDir = this.AppContext.GetExternalFilesDir(null);
        if (publicDir != null)
            this.Public = new DirectoryInfo(publicDir.AbsolutePath);

        this.store = new(this, new DefaultSerializer());
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
        => this.permissionSubject.OnNext(new PermissionRequestResult(requestCode, permissions, grantResults));

    public void Handle(Activity activity, int requestCode, Result resultCode, Intent data)
        => this.activityResultSubject.OnNext((requestCode, resultCode, data));


    public Application AppContext { get; }
    public DirectoryInfo AppData { get; }
    public DirectoryInfo Cache { get; }
    public DirectoryInfo Public { get; }


    public Activity? CurrentActivity => activityLifecycle.Activity;
    public IObservable<ActivityChanged> WhenActivityChanged() => activityLifecycle.ActivitySubject;


    readonly Handler handler = new Handler(Looper.MainLooper);
    public void InvokeOnMainThread(Action action)
    {
        if (Looper.MainLooper.IsCurrentThread)
            action();
        else
            this.handler.Post(action);
    }


    public IObservable<ActivityChanged> WhenActivityStatusChanged()
    {
        var subject = new ShinySubject<ActivityChanged>();
        if (this.CurrentActivity != null)
            subject.OnNext(new ActivityChanged(this.CurrentActivity, ActivityState.Created, null));

        activityLifecycle.ActivitySubject.Subscribe(x =>
        {
            subject.OnNext(x);
            subject.OnCompleted();
        });
        return subject;
    }


    public async Task<AccessState> RequestForegroundServicePermissions()
    {
        if (OperatingSystem.IsAndroidVersionAtLeast(33))
        {
            var results = await this.RequestPermissions(
                Manifest.Permission.ForegroundService,
                Manifest.Permission.PostNotifications
            ).ToTask();
            if (results.IsSuccess())
                return AccessState.Available;

            if (!results.IsGranted(Manifest.Permission.ForegroundService))
                return AccessState.NotSetup;

            return AccessState.Restricted; // no post_notifications
        }
        else if (OperatingSystem.IsAndroidVersionAtLeast(31))
        {
            var results = await this.RequestPermissions(Manifest.Permission.ForegroundService).ToTask();
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

    public IObservable<AccessState> RequestAccess(string androidPermission)
    {
        var subject = new ShinySubject<AccessState>();
        this.RequestPermissions(new[] { androidPermission }).Subscribe(x =>
        {
            subject.OnNext(x.IsSuccess() ? AccessState.Available : AccessState.Denied);
            subject.OnCompleted();
        });
        return subject;
    }


    public IObservable<PermissionRequestResult> RequestPermissions(params string[] androidPermissions)
    {
        var subject = new ShinySubject<PermissionRequestResult>();

        var allGood = androidPermissions.All(p => ContextCompat.CheckSelfPermission(this.AppContext, p) == Permission.Granted);
        if (allGood)
        {
            var grants = Enumerable.Repeat(Permission.Granted, androidPermissions.Length).ToArray();
            subject.OnNext(new PermissionRequestResult(0, androidPermissions, grants));
            subject.OnCompleted();
            return subject;
        }

        this.SetRequestedPermissions(androidPermissions);
        var current = Interlocked.Increment(ref this.requestCode);

        IDisposable? permSub = null;
        IDisposable? actSub = null;
        var cts = new CancellationTokenSource(TimeSpan.FromSeconds(5));

        cts.Token.Register(() =>
        {
            actSub?.Dispose();
            permSub?.Dispose();
            subject.OnError(new TimeoutException("A current activity was not detected to be able to request permissions"));
        });

        permSub = this.permissionSubject.Subscribe(x =>
        {
            if (x.RequestCode != current) return;
            cts.Cancel();
            permSub?.Dispose();
            subject.OnNext(x);
            subject.OnCompleted();
        });

        if (this.CurrentActivity != null)
        {
            ActivityCompat.RequestPermissions(this.CurrentActivity, androidPermissions, current);
        }
        else
        {
            actSub = activityLifecycle.ActivitySubject.Subscribe(x =>
            {
                actSub?.Dispose();
                ActivityCompat.RequestPermissions(x.Activity, androidPermissions, current);
            });
        }

        return subject;
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
