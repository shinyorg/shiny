using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Disposables;
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

namespace Shiny;


public partial class AndroidPlatform : IPlatform,
                                       IAndroidLifecycle.IOnActivityRequestPermissionsResult,
                                       IAndroidLifecycle.IOnActivityResult
{
    int requestCode;
    static AndroidActivityLifecycle activityLifecycle; // this should never change once installed on the platform
    readonly Subject<PermissionRequestResult> permissionSubject = new();
    readonly Subject<(int RequestCode, Result Result, Intent Intent)> activityResultSubject = new();


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


    public IObservable<ActivityChanged> WhenActivityStatusChanged() => Observable.Create<ActivityChanged>(ob =>
    {
        if (this.CurrentActivity != null)
            ob.Respond(new ActivityChanged(this.CurrentActivity, ActivityState.Created, null));

        return activityLifecycle
            .ActivitySubject
            .Subscribe(x => ob.Respond(x));
    });


    public async Task<AccessState> RequestForegroundServicePermissions()
    {
        if (OperatingSystemShim.IsAndroidVersionAtLeast(33))
        {
            var results = await this.RequestPermissions(
                Manifest.Permission.ForegroundService,
                AndroidPermissions.PostNotifications
            );
            if (results.IsSuccess())
                return AccessState.Available;

            if (!results.IsGranted(Manifest.Permission.ForegroundService))
                return AccessState.NotSetup;

            return AccessState.Restricted; // no post_notifications
        }
        else if (OperatingSystemShim.IsAndroidVersionAtLeast(31))
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

        if (OperatingSystemShim.IsAndroidVersionAtLeast(31))
            this.AppContext.StartForegroundService(intent);
        else
            this.AppContext.StartService(intent);
    }


    public void StopService(Type serviceType)
    {
        var intent = new Intent(this.AppContext, serviceType);
        intent.SetAction(ActionServiceStop);
        this.AppContext.StopService(intent);
    }


    public AccessState GetCurrentAccessState(string androidPermission)
    {
        var result = ContextCompat.CheckSelfPermission(this.AppContext, androidPermission);
        return result == Permission.Granted ? AccessState.Available : AccessState.Denied;
    }


    public IObservable<AccessState> RequestAccess(string androidPermissions)
        => this.RequestPermissions(new[] { androidPermissions }).Select(x => x.IsSuccess() ? AccessState.Available : AccessState.Denied);


    public IObservable<PermissionRequestResult> RequestPermissions(params string[] androidPermissions) => Observable.Create<PermissionRequestResult>(ob =>
    {
        var comp = new CompositeDisposable();

        //https://developer.android.com/training/permissions/requesting
        var allGood = androidPermissions.All(p => ContextCompat.CheckSelfPermission(this.AppContext, p) == Permission.Granted);
        if (allGood)
        {
            // everything is already good
            var grants = Enumerable.Repeat(Permission.Granted, androidPermissions.Length).ToArray();
            ob.Respond(new PermissionRequestResult(0, androidPermissions, grants));
        }
        else
        {
            //if (this.Status == PlatformState.Background)
            //    throw new ApplicationException("You cannot make permission requests while your application is in the background.  Please call RequestAccess in the Shiny library you are using while your app is in the foreground so your user can respond.  You are getting this message because your user has either not granted these permissions or has removed them.");

            var current = Interlocked.Increment(ref this.requestCode);
            comp.Add(this
                .permissionSubject
                .Where(x => x.RequestCode == current)
                .Subscribe(x => ob.Respond(x))
            );

            comp.Add(this
                .WhenActivityStatusChanged()
                .Take(1)
                .Timeout(TimeSpan.FromSeconds(5))
                .Subscribe(
                    x => ActivityCompat.RequestPermissions(
                        x.Activity,
                        androidPermissions,
                        current
                    ),
                    ex => ob.OnError(new TimeoutException(
                        "A current activity was not detected to be able to request permissions",
                        ex
                    ))
                )
            );
        }

        return comp;
    });
}
