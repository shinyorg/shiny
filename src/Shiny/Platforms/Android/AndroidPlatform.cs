using System;
using System.Reactive.Linq;
using System.Reactive.Subjects;
using System.Threading;
using Android.App;
using Android.Content;
using Android.Content.PM;
using AndroidX.Core.App;
using AndroidX.Lifecycle;
using Microsoft.Extensions.DependencyInjection;
using B = global::Android.OS.Build;
using Shiny.Logging;
using Shiny.Infrastructure;
using System.IO;

namespace Shiny
{
    public class AndroidPlatform : Java.Lang.Object, ILifecycleObserver, IAndroidContext, IPlatform
    {
        readonly Subject<PlatformState> stateSubj = new Subject<PlatformState>();
        readonly Subject<Intent> intentSubject = new Subject<Intent>();
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


        public DirectoryInfo AppData { get; }
        public DirectoryInfo Cache { get; }
        public DirectoryInfo Public { get; }
        public Activity? CurrentActivity => this.callbacks.Activity;
        public IObservable<ActivityChanged> WhenActivityChanged() => this.callbacks.ActivitySubject;
        [Lifecycle.Event.OnResume] public void OnResume() => this.stateSubj.OnNext(PlatformState.Foreground);
        [Lifecycle.Event.OnPause] public void OnPause() => this.stateSubj.OnNext(PlatformState.Background);
        public IObservable<PlatformState> WhenStateChanged() => this.stateSubj.OnErrorResumeNext(Observable.Empty<PlatformState>());

        public void Register(IServiceCollection services)
        {
            services.AddSingleton<IAndroidContext>(this);
            services.RegisterCommonServices();
        }


        public string AppIdentifier => this.app.PackageName;
        public string AppVersion => this.Package.VersionName;
        public string AppBuild => this.Package.VersionCode.ToString();

        public string MachineName => "Android";
        public string OperatingSystem => B.VERSION.Release;
        public string OperatingSystemVersion => B.VERSION.Sdk;
        public string Manufacturer => B.Manufacturer;
        public string Model => B.Model;


        public void OnNewIntent(Intent intent) => this.intentSubject.OnNext(intent);
        public Application AppContext => this.app;
        public IObservable<Intent> WhenIntentReceived() => this.intentSubject;
        public T GetSystemService<T>(string key) where T : Java.Lang.Object
            => (T)this.AppContext.GetSystemService(key);


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
                .WhenActivityStatusChanged()
                .Subscribe(x => ob.Respond(x));
        });


        public PackageInfo Package => this
            .AppContext
            .PackageManager
            .GetPackageInfo(this.AppContext.PackageName, 0);


        public void StartService(Type serviceType, bool foreground)
        {
            var intent = new Intent(this.AppContext, serviceType);
            if (foreground && this.IsMinApiLevel(26))
                this.AppContext.StartForegroundService(intent);
            else
                this.AppContext.StartService(intent);
        }


        public void StopService(Type serviceType)
            => this.AppContext.StopService(new Intent(this.AppContext, serviceType));


        public bool IsMinApiLevel(int apiLevel)
            => (int)B.VERSION.SdkInt >= apiLevel;


        public void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResult)
            => this.PermissionResult?.Invoke(this, new PermissionRequestResult(requestCode, permissions, grantResult));


        event EventHandler<PermissionRequestResult>? PermissionResult;


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
            var result = AndroidX.Core.Content.ContextCompat.CheckSelfPermission(this.AppContext, androidPermission);
            return result == Permission.Granted ? AccessState.Available : AccessState.Denied;
        }


        int requestCode;
        public IObservable<AccessState> RequestAccess(params string[] androidPermissions) => Observable.Create<AccessState>(ob =>
        {
            //if(ActivityCompat.ShouldShowRequestPermissionRationale(this.TopActivity, androidPermission))
            //var currentGrant = this.GetCurrentAccessState(androidPermission);
            //if (currentGrant == AccessState.Available)
            //{
            //    ob.Respond(AccessState.Available);
            //    return () => { };
            //}

            //if (!ActivityCompat.ShouldShowRequestPermissionRationale(this.AppContext, androidPermission))
            //{
            //    ob.Respond()
            //    return () => { };
            //}

            var current = Interlocked.Increment(ref this.requestCode);

            var handler = new EventHandler<PermissionRequestResult>((sender, result) =>
            {
                if (result.RequestCode != current)
                    return;

                var state = result.IsSuccess() ? AccessState.Available : AccessState.Denied;
                ob.Respond(state);
            });
            this.PermissionResult += handler;

            var sub = this.WhenActivityStatusChanged()
                .Take(1)
                .Subscribe(x =>
                    ActivityCompat.RequestPermissions(
                        x.Activity,
                        androidPermissions,
                        current
                    )
                );


            return () =>
            {
                this.PermissionResult -= handler;
                sub?.Dispose();
            };
        });


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

            Log.Write("Permissions", $"You need to declare the '{androidPermission}' in your AndroidManifest.xml");
            return false;
        }
    }
}
