using System;
using Android.App;
using Android.Content;
using Android.Content.PM;


namespace Shiny
{
    public interface IAndroidContext : IPlatform
    {
        Application AppContext { get; }
        Activity CurrentActivity { get; }
        PackageInfo Package { get; }
        IObservable<ActivityChanged> WhenActivityChanged();
        void OnNewIntent(Intent? intent);
        void OnActivityResult(int requestCode, Result resultCode, Intent data);

        Intent CreateIntent<T>(params string[] actions);
        AccessState GetCurrentAccessState(string androidPermission);
        T GetIntentValue<T>(string intentAction, Func<Intent, T> transform);
        T GetSystemService<T>(string key) where T : Java.Lang.Object;
        TValue GetSystemServiceValue<TValue, TSysType>(string systemTypeName, Func<TSysType, TValue> func) where TSysType : Java.Lang.Object;
        bool IsInManifest(string androidPermission);
        bool IsMinApiLevel(int apiLevel);
        void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResult);

        IObservable<PermissionRequestResult> RequestPermissions(params string[] androidPermissions);
        IObservable<AccessState> RequestAccess(string androidPermission);
        void StartService(Type serviceType);
        void StopService(Type serviceType);
        IObservable<ActivityChanged> WhenActivityStatusChanged();
        IObservable<Intent> WhenIntentReceived();
        IObservable<Intent> WhenIntentReceived(string intentAction);
        IObservable<(Result result, Intent data)> RequestActivityResult(Action<int, Activity> request);
    }
}