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


namespace Shiny
{
    public class AndroidPlatform : Java.Lang.Object,
                                   ILifecycleObserver,
                                   IAndroidContext,
                                   IPlatform,
                                   IPlatformBuilder
    {
        int requestCode;
        readonly Subject<PlatformState> stateSubj = new Subject<PlatformState>();
        readonly Subject<Intent> intentSubject = new Subject<Intent>();
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
            services.AddSingleton<IAndroidContext>(this);
            services.RegisterCommonServices();
        }


        public string Name => KnownPlatforms.Android;
        public DirectoryInfo AppData { get; }
        public DirectoryInfo Cache { get; }
        public DirectoryInfo Public { get; }
        public Activity? CurrentActivity => this.callbacks.Activity;
        public IObservable<ActivityChanged> WhenActivityChanged() => this.callbacks.ActivitySubject;


        [Lifecycle.Event.OnResume]
        public void OnResume()
        {
            this.Status = PlatformState.Foreground;
            this.stateSubj.OnNext(PlatformState.Foreground);
        }


        [Lifecycle.Event.OnPause]
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
                handler.Post(action);
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
        public void OnNewIntent(Intent intent) => this.intentSubject.OnNext(intent);
        public Application AppContext => this.app;
        public IObservable<Intent> WhenIntentReceived() => this.intentSubject;
        public T GetSystemService<T>(string key) where T : Java.Lang.Object
            => (T)this.AppContext.GetSystemService(key);

        public PlatformState Status { get; private set; } = PlatformState.Foreground;


        public TValue GetSystemServiceValue<TValue, TSysType>(string systemTypeName, Func<TSysType, TValue> func) where TSysType : Java.Lang.Object
        {
            using (var type = this.GetSystemService<TSysType>(systemTypeName))
                return func(type);
        }


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


        public bool IsMinApiLevel(int apiLevel)
            => (int)B.VERSION.SdkInt >= apiLevel;


        public void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResult)
            => this.permissionSubject.OnNext(new PermissionRequestResult(requestCode, permissions, grantResult));


        public T GetIntentValue<T>(string intentAction, Func<Intent, T> transform)
        {
            using (var filter = new IntentFilter(intentAction))
            using (var receiver = this.AppContext.RegisterReceiver(null, filter))
                return transform(receiver);
        }


        public IObservable<Intent> WhenIntentReceived(string intentAction)
            => Observable.Create<Intent>(ob =>
            {
                var filter = new IntentFilter();
                filter.AddAction(intentAction);
                var receiver = new ObservableBroadcastReceiver
                {
                    OnEvent = ob.OnNext
                };
                this.AppContext.RegisterReceiver(receiver, filter);
                return () => this.AppContext.UnregisterReceiver(receiver);
            });


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

            request(current, this.CurrentActivity);

            return sub;
        });


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
                if (this.Status == PlatformState.Background)
                    throw new ApplicationException("You cannot make permission requests while your application is in the background.  Please call RequestAccess in the Shiny library you are using while your app is in the foreground so your user can respond.  You are getting this message because your user has either not granted these permissions or has removed them.");

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


        public IObservable<AccessState> RequestAccess(string androidPermissions)
            => this.RequestPermissions(new[] { androidPermissions }).Select(x => x.IsSuccess() ? AccessState.Available : AccessState.Denied);


        public Intent CreateIntent<T>(params string[] actions)
        {
            var intent = new Intent(this.AppContext, typeof(T));
            foreach (var action in actions)
                intent.SetAction(action);

            return intent;
        }


        public bool IsInManifest(string androidPermission)
        {
            var permissions = this.AppContext
                .PackageManager
                .GetPackageInfo(
                    this.AppContext.PackageName,
                    PackageInfoFlags.Permissions
                )
                ?.RequestedPermissions;

            if (permissions != null)
                foreach (var permission in permissions)
                    if (permission.Equals(androidPermission, StringComparison.InvariantCultureIgnoreCase))
                        return true;

            //Log.Write("Permissions", $"You need to declare the '{androidPermission}' in your AndroidManifest.xml");
            return false;
        }
    }
}
