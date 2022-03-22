using System;
using System.IO;
#if MONOANDROID
using Android.App;
using Android.Content;
using Android.Content.PM;
#endif


namespace Shiny
{
    public enum PlatformState
    {
        Foreground,
        Background
    }


    public interface IPlatform
    {
        string Name { get; }

        PlatformState Status { get; }
        DirectoryInfo AppData { get; }
        DirectoryInfo Cache { get; }
        DirectoryInfo Public { get; }

        string AppIdentifier { get; }
        string AppVersion { get; }
        string AppBuild { get; }

        string MachineName { get; }
        string OperatingSystem { get; }
        string OperatingSystemVersion { get; }
        string Manufacturer { get; }
        string Model { get; }

        void InvokeOnMainThread(Action action);
        IObservable<PlatformState> WhenStateChanged();


        #if MONOANDROID
        Application AppContext { get; }
        Activity CurrentActivity { get; }
        PackageInfo Package { get; }
        IObservable<ActivityChanged> WhenActivityChanged();
        void OnNewIntent(Intent? intent);
        void OnActivityResult(int requestCode, Result resultCode, Intent data);

        bool IsInManifest(string androidPermission);
        bool IsMinApiLevel(int apiLevel);
        IObservable<PermissionRequestResult> RequestPermissions(params string[] androidPermissions);
        IObservable<AccessState> RequestAccess(string androidPermission);

        Intent CreateIntent<T>(params string[] actions);
        AccessState GetCurrentAccessState(string androidPermission);
        T GetIntentValue<T>(string intentAction, Func<Intent, T> transform);
        T GetSystemService<T>(string key) where T : Java.Lang.Object;
        TValue GetSystemServiceValue<TValue, TSysType>(string systemTypeName, Func<TSysType, TValue> func) where TSysType : Java.Lang.Object;
        void OnRequestPermissionsResult(int requestCode, string[] permissions, Permission[] grantResult);
        void RegisterBroadcastReceiver<T>(params string[] actions) where T : BroadcastReceiver, new();
        void StartService(Type serviceType);
        void StopService(Type serviceType);
        IObservable<ActivityChanged> WhenActivityStatusChanged();
        IObservable<Intent> WhenIntentReceived();
        IObservable<Intent> WhenIntentReceived(string intentAction);
        IObservable<(Result result, Intent data)> RequestActivityResult(Action<int, Activity> request);
        #endif
    }
}
