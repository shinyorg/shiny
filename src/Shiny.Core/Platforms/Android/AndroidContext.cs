using System;
using System.Linq;
using System.Reactive.Linq;
using System.Threading;
using Shiny.Logging;
using Android.App;
using Android.Content;
using Android.Content.PM;
using Android.Support.V4.App;
using Android.Support.V4.Content;
using NativePerm = Android.Content.PM.Permission;


namespace Shiny
{
    class AndroidContext : IAndroidContext
    {
        readonly ActivityLifecycleCallbacks callbacks;


        public AndroidContext(Application app, ActivityLifecycleCallbacks callbacks)
        {
            this.AppContext = app;
            this.callbacks = callbacks;
        }


        public Application AppContext { get; }
        public Activity CurrentActivity => this.callbacks.Activity;
        public IObservable<ActivityChanged> WhenActivityStatusChanged() => this.callbacks.ActivitySubject;
        public PackageInfo Package => this
            .AppContext
            .PackageManager
            .GetPackageInfo(this.AppContext.PackageName, 0);


        public void StartService(Type serviceType)
            => this.AppContext.StartService(new Intent(this.AppContext, serviceType));


        public void StopService(Type serviceType)
            => this.AppContext.StopService(new Intent(this.AppContext, serviceType));


        public void FirePermission(int requestCode, string[] permissions, NativePerm[] grantResult)
            => this.PermissionResult?.Invoke(this, new PermissionRequestResult(requestCode, permissions, grantResult));


        public event EventHandler<PermissionRequestResult> PermissionResult;


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
            var result = ContextCompat.CheckSelfPermission(this.AppContext, androidPermission);
            return result == Permission.Granted ? AccessState.Available : AccessState.Denied;
        }


        int requestCode;
        public IObservable<AccessState> RequestAccess(string androidPermission) => Observable.Create<AccessState>(ob =>
        {
            //if(ActivityCompat.ShouldShowRequestPermissionRationale(this.TopActivity, androidPermission))
            var currentGrant = this.GetCurrentAccessState(androidPermission);
            if (currentGrant == AccessState.Available)
            {
                ob.Respond(AccessState.Available);
                return () => { };
            }

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

            ActivityCompat.RequestPermissions(
                this.CurrentActivity,
                new[] { androidPermission },
                current
            );

            return () => this.PermissionResult -= handler;
        });


        public bool IsInManifest(string androidPermission, bool assert)
        {
            var permissions = this.AppContext.PackageManager.GetPackageInfo(
                this.AppContext.PackageName,
                PackageInfoFlags.Permissions
            )?.RequestedPermissions;

            var exists = !permissions?.Any(x => x.Equals(androidPermission, StringComparison.InvariantCultureIgnoreCase)) ?? false;
            if (!exists)
            {
                Log.Write("Permissions", $"You need to declare the '{androidPermission}' in your AndroidManifest.xml");
                return false;
            }

            return true;
        }
    }
}
