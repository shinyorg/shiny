using System;
using Android.App;
using Android.Content;
using Android.Content.PM;


namespace Shiny
{
    public interface IAndroidContext
    {
        Application AppContext { get; }
        Activity CurrentActivity { get; }
        PackageInfo Package { get; }
        IObservable<ActivityChanged> WhenActivityChanged();
        void OnNewIntent(Intent intent);

        Intent CreateIntent<T>(params string[] actions);
        AccessState GetCurrentAccessState(string androidPermission);
        T GetIntentValue<T>(string intentAction, Func<Intent, T> transform);
        T GetSystemService<T>(string key) where T : Java.Lang.Object;
        TValue GetSystemServiceValue<TValue, TSysType>(string systemTypeName, Func<TSysType, TValue> func) where TSysType : Java.Lang.Object;
        bool IsInManifest(string androidPermission);
        bool IsMinApiLevel(int apiLevel);
        void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResult);
        IObservable<AccessState> RequestAccess(params string[] androidPermissions);
        void StartService(Type serviceType, bool foreground);
        void StopService(Type serviceType);
        IObservable<ActivityChanged> WhenActivityStatusChanged();
        IObservable<Intent> WhenIntentReceived();
        IObservable<Intent> WhenIntentReceived(string intentAction);
    }
}