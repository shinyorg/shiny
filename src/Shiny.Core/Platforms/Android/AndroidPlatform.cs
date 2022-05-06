using System;
using System.IO;
using System.Linq;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Reactive.Disposables;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.OS;
using AndroidX.Core.App;
using AndroidX.Core.Content;
using AndroidX.Lifecycle;
using B = global::Android.OS.Build;
using Microsoft.Extensions.DependencyInjection;
using Java.Interop;


namespace Shiny
{
    public class AndroidPlatform : Java.Lang.Object,
                                   ILifecycleObserver,
                                   IPlatform
    {
        int requestCode;
        readonly Subject<PlatformState> stateSubj = new Subject<PlatformState>();
        readonly Subject<Intent> newIntentSubject = new Subject<Intent>();
        readonly Subject<PermissionRequestResult> permissionSubject = new Subject<PermissionRequestResult>();
        readonly Subject<(int RequestCode, Result Result, Intent Intent)> activityResultSubject = new Subject<(int RequestCode, Result Result, Intent Intent)>();
        readonly Application app;
        readonly ActivityLifecycleCallbacks callbacks;


        public AndroidPlatform(Application app)
        {
            this.app = app;
            this.callbacks = new ActivityLifecycleCallbacks();
            this.app.RegisterActivityLifecycleCallbacks(this.callbacks);
            ProcessLifecycleOwner.Get().Lifecycle.AddObserver(this);

            this.AppData = new DirectoryInfo(this.AppContext.FilesDir.AbsolutePath);
            this.Cache = new DirectoryInfo(this.AppContext.CacheDir.AbsolutePath);
            var publicDir = this.app.GetExternalFilesDir(null);
            if (publicDir != null)
                this.Public = new DirectoryInfo(publicDir.AbsolutePath);
        }


        public void Register(IServiceCollection services)
        {
            services.AddSingleton<IPlatform>(this);
            services.RegisterCommonServices();
        }


        public DirectoryInfo AppData { get; }
        public DirectoryInfo Cache { get; }
        public DirectoryInfo Public { get; }
        public Activity? CurrentActivity => this.callbacks.Activity;
        public IObservable<ActivityChanged> WhenActivityChanged() => this.callbacks.ActivitySubject;


        [Lifecycle.Event.OnResume]
        [Export]
        public void OnResume()
        {
            this.Status = PlatformState.Foreground;
            this.stateSubj.OnNext(PlatformState.Foreground);
        }


        [Lifecycle.Event.OnPause]
        [Export]
        public void OnPause()
        {
            this.Status = PlatformState.Background;
            this.stateSubj.OnNext(PlatformState.Background);
        }


        public IObservable<PlatformState> WhenStateChanged()
            => this.stateSubj.OnErrorResumeNext(Observable.Empty<PlatformState>());


        readonly Handler handler = new Handler(Looper.MainLooper);
        public void InvokeOnMainThread(Action action)
        {
            if (Looper.MainLooper.IsCurrentThread)
                action();
            else
                this.handler.Post(action);
        }


        public string AppIdentifier => this.app.PackageName;
        public string AppVersion => this.Package.VersionName;
        public string AppBuild => this.Package.VersionCode.ToString();

        public string MachineName => B.GetSerial();
        public string OperatingSystem => B.VERSION.Release;
        public string OperatingSystemVersion => B.VERSION.Sdk;
        public string Manufacturer => B.Manufacturer;
        public string Model => B.Model;

        public void OnActivityResult(int requestCode, Result resultCode, Intent data) => this.activityResultSubject.OnNext((requestCode, resultCode, data));
        public void OnNewIntent(Intent intent) => this.newIntentSubject.OnNext(intent);
        public Application AppContext => this.app;

        
        public IObservable<(bool NewIntent, Intent Intent)> WhenIntentReceived() => Observable.Create<(bool NewIntent, Intent Intent)>(ob =>
        {
            var comp = new CompositeDisposable();

            this.WhenActivityChanged()
                .Where(x =>
                    x.Status == ActivityState.Resumed &&
                    x.Activity.Intent != null
                )
                .Subscribe(x => ob.OnNext((false, x.Activity.Intent!)))
                .DisposedBy(comp);

            this.newIntentSubject
                .Subscribe(intent => ob.OnNext((true, intent)))
                .DisposedBy(comp);

            return comp;
       });  

        public PlatformState Status { get; private set; } = PlatformState.Foreground;


        public IObservable<ActivityChanged> WhenActivityStatusChanged() => Observable.Create<ActivityChanged>(ob =>
        {
            if (this.CurrentActivity != null)
                ob.Respond(new ActivityChanged(this.CurrentActivity, ActivityState.Created, null));

            return this
                .callbacks
                .ActivitySubject
                .Subscribe(x => ob.Respond(x));
        });


        public PackageInfo Package => this
            .AppContext
            .PackageManager
            .GetPackageInfo(this.AppContext.PackageName, 0);


        public void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResult)
            => this.permissionSubject.OnNext(new PermissionRequestResult(requestCode, permissions, grantResult));


        public const string ActionServiceStart = "ACTION_START_FOREGROUND_SERVICE";
        public const string ActionServiceStop = "ACTION_STOP_FOREGROUND_SERVICE";

        public void StartService(Type serviceType)
        {
            //ActionServiceStart
            var intent = new Intent(this.AppContext, serviceType);
            if (this.IsMinApiLevel(26) && this.IsShinyForegroundService(serviceType))
            {
                intent.SetAction(ActionServiceStart);
                this.AppContext.StartForegroundService(intent);
            }
            else
            {
                this.AppContext.StartService(intent);
            }
        }


        public void StopService(Type serviceType)
        {
            if (!this.IsShinyForegroundService(serviceType))
            {
                this.AppContext.StopService(new Intent(this.AppContext, serviceType));
            }
            else
            {
                // HACK: this re-runs the intent to stop the service since OnTaskRemoved isn't running
                var intent = new Intent(this.AppContext, serviceType);
                intent.SetAction(ActionServiceStop);
                this.AppContext.StartService(intent);
            }
        }

        protected bool IsShinyForegroundService(Type serviceType)
            => serviceType?.BaseType.Name.Contains("ShinyAndroidForegroundService") ?? false;


        public AccessState GetCurrentAccessState(string androidPermission)
        {
            var result = ContextCompat.CheckSelfPermission(this.AppContext, androidPermission);
            return result == Permission.Granted ? AccessState.Available : AccessState.Denied;
        }


        public IObservable<(Result result, Intent data)> RequestActivityResult(Action<int, Activity> request) => Observable.Create<(Result result, Intent data)>(ob =>
        {
            var current = Interlocked.Increment(ref this.requestCode);
            var sub = this.activityResultSubject
                .Where(x => x.RequestCode == current)
                .Subscribe(x => ob.Respond((x.Result, x.Intent)));

            request(current, this.CurrentActivity!);

            return sub;
        });


        public bool IsMinApiLevel(int apiLevel)
            => (int)B.VERSION.SdkInt >= apiLevel;


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
                    .Subscribe(x =>
                        ActivityCompat.RequestPermissions(
                            x.Activity,
                            androidPermissions,
                            current
                        )
                    )
                );
            }

            return comp;
        });
    }
}
