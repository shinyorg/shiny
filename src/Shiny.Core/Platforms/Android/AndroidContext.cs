using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using System.Reactive.Subjects;
using Shiny.Logging;
using Android.App;
using Android.Content;
using Android.Content.PM;
using NativePerm = Android.Content.PM.Permission;


namespace Shiny
{
    public class AndroidContext
    {
        readonly ITopActivity topActivity;


        public AndroidContext(Application app, ITopActivity topActivity)
        {
            this.AppContext = app;
            this.topActivity = topActivity;
        }


        public Application AppContext { get; }
        public Activity CurrentActivity
        {
            get
            {
                if (this.topActivity.Current == null)
                    throw new ApplicationException("TopActivity could not be found - are you calling this before an activity has been created or resumed?");

                return this.topActivity.Current;
            }
        }


        internal Subject<Intent> IntentSubject { get; } = new Subject<Intent>();
        public IObservable<Intent> WhenIntentReceived() => this.IntentSubject;

        public T GetSystemService<T>(string key) where T: Java.Lang.Object
            => (T)this.AppContext.GetSystemService(key);


        public TValue GetSystemServiceValue<TValue, TSysType>(string systemTypeName, Func<TSysType, TValue> func) where TSysType : Java.Lang.Object
        {
            using (var type = this.GetSystemService<TSysType>(systemTypeName))
                return func(type);
        }


        public IObservable<ActivityChanged> WhenActivityStatusChanged() => Observable.Create<ActivityChanged>(ob =>
        {
            if (this.topActivity.Current != null)
                ob.Respond(new ActivityChanged(this.topActivity.Current, ActivityState.Created, null));

            return this
                .topActivity
                .WhenActivityStatusChanged()
                .Subscribe(x => ob.Respond(x));
        });


        public PackageInfo Package => this
            .AppContext
            .PackageManager
            .GetPackageInfo(this.AppContext.PackageName, 0);


        public void StartService(Type serviceType)
            => this.AppContext.StartService(new Intent(this.AppContext, serviceType));


        public void StopService(Type serviceType)
            => this.AppContext.StopService(new Intent(this.AppContext, serviceType));


        public bool IsMinApiLevel(int apiLevel)
            => (int)Android.OS.Build.VERSION.SdkInt >= apiLevel;


        public void FirePermission(int requestCode, string[] permissions, NativePerm[] grantResult)
            => this.PermissionResult?.Invoke(this, new PermissionRequestResult(requestCode, permissions, grantResult));


        public event EventHandler<PermissionRequestResult>? PermissionResult;


        //public IObservable<Configuration> WhenConfigurationChanged() => this
        //    .WhenIntentReceived(Intent.ActionConfigurationChanged)
        //    .Select(intent => this.AppContext.Resources.Configuration);


        //public PendingIntent GetIntentServicePendingIntent()
        //{
        //    var intent = new Intent(Application.Context, typeof(CoreIntentService));
        //    var pendingIntent = PendingIntent.GetService(this.AppContext, 0, intent, PendingIntentFlags.UpdateCurrent);
        //    return pendingIntent;
        //}


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
#if !ANDROIDX
            var result = Android.Support.V4.Content.ContextCompat.CheckSelfPermission(this.AppContext, androidPermission);
            return result == Permission.Granted ? AccessState.Available : AccessState.Denied;
#else
            var result = AndroidX.Core.Content.ContextCompat.CheckSelfPermission(this.AppContext, androidPermission);
            return result == Permission.Granted ? AccessState.Available : AccessState.Denied;
#endif
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
#if !ANDROIDX
                    Android.Support.V4.App.ActivityCompat.RequestPermissions(
                        x.Activity,
                        androidPermissions,
                        current
                    )
#else
                    AndroidX.Core.App.ActivityCompat.RequestPermissions(
                        x.Activity,
                        androidPermissions,
                        current
                    )
#endif
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
