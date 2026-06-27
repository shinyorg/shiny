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
    const char PermissionsSeparator = '';
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
        var stored = this.store.Get<string>(PermissionsKey);
        this.requestedPermissions = string.IsNullOrEmpty(stored)
            ? new List<string>()
            : new List<string>(stored.Split(PermissionsSeparator, StringSplitOptions.RemoveEmptyEntries));
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

        // we need an activity to present the OS permission dialog.  If one isn't available yet
        // (eg. the request fired before the UI was up), wait briefly for one to appear.  This
        // timeout ONLY covers acquiring the activity - it must NEVER cover the time the user
        // spends interacting with the dialog, otherwise a slow tap throws after 5s and crashes
        // the app while the dialog is still on screen (see issue #1625).
        var activity = this.CurrentActivity;
        if (activity == null)
        {
            using var actCts = CancellationTokenSource.CreateLinkedTokenSource(cancellationToken);
            actCts.CancelAfter(TimeSpan.FromSeconds(5));
            try
            {
                var change = await this
                    .WaitForActivity(ActivityState.Resumed, actCts.Token)
                    .ConfigureAwait(false);

                activity = change.Activity;
            }
            catch (System.OperationCanceledException) when (!cancellationToken.IsCancellationRequested)
            {
                throw new TimeoutException("A current activity was not detected to be able to request permissions");
            }
        }

        var current = Interlocked.Increment(ref this.requestCode);
        var tcs = new TaskCompletionSource<PermissionRequestResult>();
        this.pendingPermissions[current] = tcs;

        // honor caller-driven cancellation while waiting for the user's response, but apply
        // NO wall-clock timeout - the user may take as long as they like in the dialog.
        using var reg = cancellationToken.Register(() =>
        {
            if (this.pendingPermissions.TryRemove(current, out var t))
                t.TrySetCanceled(cancellationToken);
        });

        ActivityCompat.RequestPermissions(activity, androidPermissions, current);

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
                this.store.Set(PermissionsKey, string.Join(PermissionsSeparator, this.requestedPermissions));
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
